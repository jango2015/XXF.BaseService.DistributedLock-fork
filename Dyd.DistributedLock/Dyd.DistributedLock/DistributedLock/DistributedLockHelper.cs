using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XXF.BaseService.DistributedLock.RedisLock;
using XXF.BaseService.DistributedLock.SystemRuntime;
using XXF.Extensions;

namespace XXF.BaseService.DistributedLock
{
    /// <summary>
    /// 分布式锁使用帮助类
    /// </summary>
    public class DistributedLockHelper
    {
        /// <summary>
        /// 分布式尝试锁
        /// </summary>
        /// <param name="info"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public static LockResult TryLock(BaseDistributedLockInfo info, Action action)
        {
            try
            {
                if (info is RedisDistributedLockInfo)
                {
                    var redisinfo = info as RedisDistributedLockInfo;
                    if (string.IsNullOrEmpty(DistributedLockConfig.DistributedLockRedisServers))
                    {
                        throw new DistributedLockException("请在web.config或app.config中配置DistributedLockRedisServers");
                    }
                    var redisserver = new RedisLoadBalance(DistributedLockConfig.GetDistributedLockRedisServerList()).ChooseRedisServer(info.DistributedLockKey);

                    using (var @lock = new RedisDistributedLockFromJava(redisserver, info.DistributedLockKey))
                    {
                        var islock = @lock.TryGetDistributedLock(info.GetLockTimeOut, info.TaskRunTimeOut);
                        if (islock == LockResult.Success)
                            action();
                        return islock;
                    }

                }
                throw new DistributedLockException("info 参数传入错误");
            }
            catch (Exception exp)
            {
                XXF.Log.ErrorLog.Write(string.Format("分布式尝试锁错误,key:{0}", (info == null ? "" : info.DistributedLockKey.NullToEmpty())), exp);
                throw exp;
            }
        }

    }
    /// <summary>
    /// 分布式锁基本信息
    /// </summary>
    public abstract class BaseDistributedLockInfo
    {
        /// <summary>
        /// 分布式锁关键标识
        /// 备注:内部会采用关键标识hash做负载均衡
        /// </summary>
        public string DistributedLockKey { get; set; }
        /// <summary>
        /// 不断尝试获取锁的超时的时间;null 表示 仅尝试"一次"获取锁,若失败就放弃尝试。
        /// </summary>
        public TimeSpan? GetLockTimeOut { get; set; }
        /// <summary>
        /// 定义任务执行最大超时时间（即任务锁最多保留的时间）,默认情况下任务锁被获取，任务执行完毕立即释放。但是若任务执行期间异常等情况，任务锁未被释放并被一直保留在分布式内存中。
        /// null表示系统最大执行时间(默认为30s);超过这个执行时间后,任务锁将被系统自动释放;一旦被自动回收，即便任务依旧在运行中,其他竞争者依旧可以获取锁并执行任务。
        /// 另外任务锁保留期间,任何对锁的获取都会超时失败。
        /// 一般情况下建议GetLockTimeOut>=TaskRunTimeOut,这样就能保证锁一定能被获取到，无非等待时间较长，具体看业务情况而定。
        /// </summary>
        public TimeSpan? TaskRunTimeOut { get; set; }
    }
    /// <summary>
    /// redis分布式锁基本信息
    /// </summary>
    public class RedisDistributedLockInfo : BaseDistributedLockInfo
    {

    }
}
