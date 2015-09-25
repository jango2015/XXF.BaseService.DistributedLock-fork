using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XXF.BaseService.DistributedLock.SystemRuntime
{
    /// <summary>
    /// 获取锁结果枚举
    /// </summary>
    public enum LockResult
    {
        /// <summary>
        /// 获取锁成功
        /// </summary>
        Success,
        /// <summary>
        /// 获取锁超时失败
        /// </summary>
        GetLockTimeOutFailure,
        /// <summary>
        /// 锁服务系统级出错导致失败,一般来说是分布式锁服务出现了异常，不能提供服务了
        /// </summary>
        LockSystemExceptionFailure,
    }
}
