using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.Redis;
using XXF.BaseService.DistributedLock.SystemRuntime;

namespace XXF.BaseService.DistributedLock.RedisLock
{
    public abstract class BaseRedisDistributedLock:IDisposable
    {
        protected string redisserver;
        protected string key;
        protected LockResult lockresult = LockResult.LockSystemExceptionFailure;
        public BaseRedisDistributedLock(string redisserver, string key)
        {
            this.redisserver = redisserver;
            this.key = key;
         }

         /// <summary>
        /// 尝试获取分布式锁
        /// </summary>
        /// <param name="timeOut"></param>
        /// <returns></returns>
         public virtual LockResult TryGetDistributedLock(TimeSpan? getlockTimeOut,TimeSpan? taskrunTimeOut)
         {
             if (lockresult == LockResult.Success)
                 throw new DistributedLockException("检测到当前锁已获取");
             lockresult = LockResult.LockSystemExceptionFailure;
             return lockresult;
         }

         public virtual void Dispose() { }
    }
}
