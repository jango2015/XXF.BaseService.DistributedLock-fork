using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XXF.BaseService.DistributedLock.SystemRuntime;
using ServiceStack.Common;
using ServiceStack.Text;
using XXF.Extensions;

namespace XXF.BaseService.DistributedLock.RedisLock
{
    /*
    * Redis分布式锁
    * 采用ServiceStack.Redis实现的Redis分布式锁
    * 详情可阅读其开源代码
    * 备注：不同版本的 ServiceStack.Redis 实现reidslock机制不同 
     * 拷贝自网络开源代码 较旧的实现版本
    */
    public class RedisDistributedLockFromServiceStack : BaseRedisDistributedLock
    {
        public RedisDistributedLockFromServiceStack(string redisserver, string key)
            : base(redisserver, key)
        {


        }
        public override LockResult TryGetDistributedLock(TimeSpan? getlockTimeOut, TimeSpan? taskrunTimeOut)
        {
            if (lockresult == LockResult.Success)
                throw new DistributedLockException("检测到当前锁已获取");
            try
            {
                using (var redisClient = DistributedLockConfig.GetRedisPoolClient(redisserver).GetClient())
                {
                    ExecExtensions.RetryUntilTrue(
                             () =>
                             {
                                 //This pattern is taken from the redis command for SETNX http://redis.io/commands/setnx

                                 //Calculate a unix time for when the lock should expire
                                 TimeSpan realSpan = taskrunTimeOut ?? TimeSpan.FromMilliseconds(DistributedLockConfig.MaxLockTaskRunTime); //new TimeSpan(365, 0, 0, 0); //if nothing is passed in the timeout hold for a year
                                 DateTime expireTime = DateTime.UtcNow.Add(realSpan);
                                 string lockString = (expireTime.ToUnixTimeMs() + 1).ToString();

                                 //Try to set the lock, if it does not exist this will succeed and the lock is obtained
                                 var nx = redisClient.SetEntryIfNotExists(key, lockString);
                                 if (nx)
                                 {
                                     lockresult = LockResult.Success;
                                     return true;
                                 }

                                 //If we've gotten here then a key for the lock is present. This could be because the lock is
                                 //correctly acquired or it could be because a client that had acquired the lock crashed (or didn't release it properly).
                                 //Therefore we need to get the value of the lock to see when it should expire

                                 redisClient.Watch(key);
                                 string lockExpireString = redisClient.Get<string>(key);
                                 long lockExpireTime;
                                 if (!long.TryParse(lockExpireString, out lockExpireTime))
                                 {
                                     redisClient.UnWatch();  // since the client is scoped externally
                                     lockresult = LockResult.GetLockTimeOutFailure;
                                     return false;
                                 }

                                 //If the expire time is greater than the current time then we can't let the lock go yet
                                 if (lockExpireTime > DateTime.UtcNow.ToUnixTimeMs())
                                 {
                                     redisClient.UnWatch();  // since the client is scoped externally
                                     lockresult = LockResult.GetLockTimeOutFailure;
                                     return false;
                                 }

                                 //If the expire time is less than the current time then it wasn't released properly and we can attempt to 
                                 //acquire the lock. The above call to Watch(_lockKey) enrolled the key in monitoring, so if it changes
                                 //before we call Commit() below, the Commit will fail and return false, which means that another thread 
                                 //was able to acquire the lock before we finished processing.
                                 using (var trans = redisClient.CreateTransaction()) // we started the "Watch" above; this tx will succeed if the value has not moved 
                                 {
                                     trans.QueueCommand(r => r.Set(key, lockString));
                                     //return trans.Commit(); //returns false if Transaction failed
                                     var t = trans.Commit();
                                     if (t == false)
                                         lockresult = LockResult.GetLockTimeOutFailure;
                                     else
                                         lockresult = LockResult.Success;
                                     return t;
                                 }
                             },
                             getlockTimeOut
                         );

                }
            }
            catch (Exception exp)
            {
                XXF.Log.ErrorLog.Write(string.Format("redis分布式尝试锁系统级别严重异常,redisserver:{0}", redisserver.NullToEmpty()), exp);
                lockresult = LockResult.LockSystemExceptionFailure;
            }
            return lockresult;
        }

        public override void Dispose()
        {
            try
            {
                using (var redisClient = DistributedLockConfig.GetRedisPoolClient(redisserver).GetClient())
                {
                    redisClient.Remove(key);
                }
            }
            catch (Exception exp)
            {
                XXF.Log.ErrorLog.Write(string.Format("redis分布式尝试锁释放严重异常,redisserver:{0}", redisserver.NullToEmpty()), exp);
            }
        }
    }
}
