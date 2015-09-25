using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XXF.BaseService.DistributedLock.RedisLock
{
    /// <summary>
    /// Redis简单负载均衡算法
    /// </summary>
    public class RedisLoadBalance
    {
        public List<string> redisServers;
        public RedisLoadBalance(List<string> redisservers)
        {
            redisServers=redisservers;
        }
        public string ChooseRedisServer(string key)
        {
            if (redisServers == null || redisServers.Count == 0)
                throw new SystemRuntime.DistributedLockException("当前reids服务器列表为空");
            int hashcode = Math.Abs( key.GetHashCode());
            int index = hashcode % redisServers.Count;
            return redisServers[index];
        }
    }
}
