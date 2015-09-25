using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XXF.BaseService.DistributedLock.SystemRuntime
{
    /// <summary>
    /// 分布式锁错误
    /// </summary>
    public class DistributedLockException:Exception
    {
        public DistributedLockException(string message):base(message)
        {
 
        }
    }
}
