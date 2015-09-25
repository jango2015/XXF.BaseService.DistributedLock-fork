using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.Redis;
using XXF.BaseService.DistributedLock.SystemRuntime;
using XXF.Extensions;

namespace XXF.BaseService.DistributedLock.RedisLock
{
    /*
     * Redis分布式锁
     * 采用ServiceStack.Redis实现的Redis分布式锁
     * 详情可阅读其开源代码
     * 备注：不同版本的 ServiceStack.Redis 实现reidslock机制不同 xxf里面默认使用2.2版本
     */
    public class RedisDistributedLock : BaseRedisDistributedLock
    {
        private ServiceStack.Redis.RedisLock _lock;
        private RedisClient _client;
        public RedisDistributedLock(string redisserver, string key)
            : base(redisserver, key)
        {

        }

        public override LockResult TryGetDistributedLock(TimeSpan? getlockTimeOut, TimeSpan? taskrunTimeOut)
        {
            if (lockresult == LockResult.Success)
                throw new DistributedLockException("检测到当前锁已获取");
            _client = DistributedLockConfig.GetRedisPoolClient(redisserver).GetClient();
            /*
             * 阅读源码发现当其获取锁后,redis连接资源会一直占用,知道获取锁的资源释放后,连接才会跳出，可能会导致连接池资源的浪费。
             */
            try
            {
                this._lock = new ServiceStack.Redis.RedisLock(_client, key, getlockTimeOut);
                lockresult =  LockResult.Success;
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
                if (this._lock != null)
                    this._lock.Dispose();
                if (_client != null)
                    this._client.Dispose();
            }
            catch (Exception exp)
            {
                XXF.Log.ErrorLog.Write(string.Format("redis分布式尝试锁释放严重异常,redisserver:{0}", redisserver.NullToEmpty()), exp);
            }
        }
    }
}
