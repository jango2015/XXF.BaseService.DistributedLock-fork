using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XXF.BaseService.DistributedLock.SystemRuntime;
using ServiceStack.Common;
using ServiceStack.Redis;
using XXF.Redis;
using XXF.Extensions;

namespace XXF.BaseService.DistributedLock.RedisLock
{
    /*
     * Redis分布式锁
     * 采用网络上java实现的Redis分布式锁
     * 参考 http://www.blogjava.net/hello-yun/archive/2014/01/15/408988.html
     * 详情可阅读其开源代码
     */
    public class RedisDistributedLockFromJava : BaseRedisDistributedLock
    {


        public RedisDistributedLockFromJava(string redisserver, string key)
            : base(redisserver, key)
        {


        }

        public override LockResult TryGetDistributedLock(TimeSpan? getlockTimeOut, TimeSpan? taskrunTimeOut)
        {
            if (lockresult == LockResult.Success)
                throw new DistributedLockException("检测到当前锁已获取");
            try
            {
                // 1. 通过SETNX试图获取一个lock
                string @lock = key;
                long taskexpiredMilliseconds = (taskrunTimeOut != null ? (long)taskrunTimeOut.Value.TotalMilliseconds : (long)DistributedLockConfig.MaxLockTaskRunTime);
                long getlockexpiredMilliseconds = (getlockTimeOut != null ? (long)getlockTimeOut.Value.TotalMilliseconds : 0);
                long hassleepMilliseconds = 0;
                while (true)
                {
                    using (var redisclient = DistributedLockConfig.GetRedisPoolClient(redisserver).GetClient())
                    {
                        long value = CurrentUnixTimeMillis() + taskexpiredMilliseconds + 1;
                        /*Java以前版本都是用SetNX,但是这种是无法设置超时时间的,不是很理解为什么,
                         * 可能是因为原来的redis命令比较少导致的？现在用Add不知道效果如何.
                         因对redis细节不了解，但个人怀疑若异常未释放锁经常发生，可能会导致内存逐步溢出*/
                        bool acquired = redisclient.Add<long>(@lock, value, TimeSpan.FromMilliseconds(taskexpiredMilliseconds + DistributedLockConfig.TaskLockDelayCleepUpTime));
                        //SETNX成功，则成功获取一个锁
                        if (acquired == true)
                        {
                            lockresult = LockResult.Success;
                        }
                        //SETNX失败，说明锁仍然被其他对象保持，检查其是否已经超时
                        else
                        {
                            var oldValueBytes = redisclient.Get(@lock);
                            //超时
                            if (oldValueBytes != null && BitConverter.ToInt64(oldValueBytes, 0) < CurrentUnixTimeMillis())
                            {
                                /*此处虽然重设并获取锁,但是超时时间可能被覆盖,故重设超时时间;若有进程一直在尝试获取锁，那么锁存活时间应该被延迟*/
                                var getValueBytes = redisclient.GetSet(@lock, BitConverter.GetBytes(value));
                                var o1 = redisclient.ExpireEntryIn(@lock, TimeSpan.FromMilliseconds(taskexpiredMilliseconds + DistributedLockConfig.TaskLockDelayCleepUpTime));//这里如果程序异常终止，依然会有部分锁未释放的情况。
                                // 获取锁成功
                                if (getValueBytes == oldValueBytes)
                                {
                                    lockresult = LockResult.Success;
                                }
                                // 已被其他进程捷足先登了
                                else
                                {
                                    lockresult = LockResult.GetLockTimeOutFailure;
                                }
                            }
                            //未超时，则直接返回失败
                            else
                            {
                                lockresult = LockResult.GetLockTimeOutFailure;
                            }
                        }
                    }

                    //成功拿到锁
                    if (lockresult == LockResult.Success)
                        break;

                    //获取锁超时
                    if (hassleepMilliseconds >= getlockexpiredMilliseconds)
                    {
                        lockresult = LockResult.GetLockTimeOutFailure;
                        break;
                    }

                    //继续等待
                    System.Threading.Thread.Sleep(DistributedLockConfig.GetLockFailSleepTime);
                    hassleepMilliseconds += DistributedLockConfig.GetLockFailSleepTime;
                }
            }
            catch (Exception exp)
            {
                XXF.Log.ErrorLog.Write(string.Format("redis分布式尝试锁系统级别严重异常,redisserver:{0}", redisserver.NullToEmpty()), exp);
                lockresult = LockResult.LockSystemExceptionFailure;
            }
            return lockresult;
        }

        private long CurrentUnixTimeMillis()
        {
            return (long)(System.DateTime.UtcNow - new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc)).TotalMilliseconds;
        }

        public override void Dispose()
        {
            if (lockresult == LockResult.Success || lockresult == LockResult.LockSystemExceptionFailure)
            {
                try
                {
                    long current = CurrentUnixTimeMillis();
                    using (var redisclient = DistributedLockConfig.GetRedisPoolClient(redisserver).GetClient())
                    {
                        var v = redisclient.Get(key);
                        if (v != null)

                        {
                            // 避免删除非自己获取得到的锁
                            if (current < BitConverter.ToInt64(v, 0))
                            {
                                redisclient.Del(key);
                            }
                        }
                    }
                }
                catch (Exception exp)
                {
                    XXF.Log.ErrorLog.Write(string.Format("redis分布式尝试锁释放严重异常,redisserver:{0}", redisserver.NullToEmpty()), exp);
                }
            }
        }
    }
}
