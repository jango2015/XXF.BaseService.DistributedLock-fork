using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XXF.BaseService.DistributedLock.SystemRuntime
{
    /// <summary>
    /// 分布式锁相关系统配置
    /// </summary>
    public class DistributedLockConfig
    {
        /// <summary>
        /// 分布式锁redis服务器,分隔多台服务器地址
        /// </summary>
        public static string DistributedLockRedisServers = XXF.Common.XXFConfig.Get("DistributedLockRedisServers", "");
        /// <summary>
        /// 分布式锁服务器最大客户端连接池
        /// </summary>
        public static int DistributedLockRedisServersMaxPoolClient = Convert.ToInt32(XXF.Common.XXFConfig.Get("DistributedLockRedisServersMaxPoolClient", "2"));
        /// <summary>
        /// 获取分布式锁redis服务器列表
        /// </summary>
        /// <returns></returns>
        public static List<string> GetDistributedLockRedisServerList()
        {
            string[] servers = DistributedLockRedisServers.Trim(',').Split(',');
            return new List<string>(servers);
        }
        /// <summary>
        /// 获取redis连接池客户端
        /// </summary>
        /// <param name="redisserver"></param>
        /// <returns></returns>
        public static XXF.Redis.RedisDb GetRedisPoolClient(string redisserver)
        {
            return new XXF.Redis.RedisManager().GetPoolClient(redisserver, DistributedLockConfig.DistributedLockRedisServersMaxPoolClient, DistributedLockConfig.DistributedLockRedisServersMaxPoolClient);
        }

        public static long MaxLockTaskRunTime = (long)TimeSpan.FromMilliseconds(1000*30).TotalMilliseconds;//锁住任务运行最大时间为30s 超过这个时间,任务被强制回收
        public static long TaskLockDelayCleepUpTime = (long)TimeSpan.FromSeconds(1).TotalMilliseconds;//任务锁在内存中延迟回收时间
        public static int GetLockFailSleepTime = (int)TimeSpan.FromMilliseconds(10).TotalMilliseconds;
    }
}
