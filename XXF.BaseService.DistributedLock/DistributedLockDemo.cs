using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XXF.BaseService.DistributedLock
{
     /*
     *分布式锁使用demo
     */
    public abstract class DistributedLockDemo
    {
        public void Demo()
        {
            /*
             * 先进行必要的业务去重逻辑,进行一些必要的过滤,避免分布式锁产生大量的锁。
             */

            var action = new Action(()=>{
             /*
              * 这里是需要加锁的业务逻辑,加锁的业务逻辑应该细小，并在加锁前要进行一些必要的业务去重复。尽量减少重复锁的几率。
              */
            });

            /* 备注:分布式锁是一个全局性的稀有资源，一旦加锁或者锁的粒度较大,对于分布式的应用程序来说性能是非常差的。对于业务来说，我们优先考虑不采用锁，通过业务协调方式解决。
            * 在一些必须，强一致性的情况下才考虑锁的使用。需要加锁的业务逻辑应该是细小的, 加锁的前要过滤一部分的重复业务逻辑，这样锁才不会很频繁*/

            var r = DistributedLockHelper.TryLock(new RedisDistributedLockInfo() { 
                DistributedLockKey = "aaa4", //业务锁标记关键词
                GetLockTimeOut = TimeSpan.FromMilliseconds(50),//获取锁的超时时间，超时时间内未获取锁,则返回 GetLockTimeOutFailure
                TaskRunTimeOut = TimeSpan.FromMilliseconds(50)//定义任务最大的执行时间，超过这个时间,任务产生的锁将被系统自动回收，一旦被自动回收，即便任务依旧在运行中,其他竞争者依旧可以获取锁。
            },//
                action) ;

            //分布式锁的服务出现异常了，不能提供服务了
            if (r == DistributedLock.SystemRuntime.LockResult.LockSystemExceptionFailure)
            {
                //有些线上业务不能因为锁服务异常终止，则需要再次检查一般业务重复情况，然后假设默认获取了锁，再执行一般业务
                /*
                 * 再次检测业务是否重复
                 */
                action();
            }
            //分布式锁获取超时了,备注:在获取锁的时间内,当前任务的锁未释放
            else if (r == DistributedLock.SystemRuntime.LockResult.GetLockTimeOutFailure)
            {
                //可以继续等待获取锁,也可以直接选择放弃获取锁,放弃下面整个业务流程
                /*
                 * DistributedLockHelper.TryLock(new RedisDistributedLockInfo() { 
                        DistributedLockKey = "aaa4", 
                        GetLockTimeOut=TimeSpan.FromSeconds(1),
                        TaskRunTimeOut=TimeSpan.FromSeconds(5)},action) ;
                 */
                return;
            }
              
            /*
             获取锁后继续业务流程
             * 
             * 
             * 
             */
        }
    }
}
