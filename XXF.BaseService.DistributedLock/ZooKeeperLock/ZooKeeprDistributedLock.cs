using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ZooKeeperNet;

namespace XXF.BaseService.DistributedLock.ZooKeeperLock
{
 /*
  * 来源java网络源码的zookeeper分布式锁实现（目前仅翻译并简单测试ok，未来集成入sdk）
  * 备注:
    共享锁在同一个进程中很容易实现，但是在跨进程或者在不同 Server 之间就不好实现了。Zookeeper 却很容易实现这个功能，实现方式也是需要获得锁的 Server 创建一个 EPHEMERAL_SEQUENTIAL 目录节点，
    然后调用 getChildren方法获取当前的目录节点列表中最小的目录节点是不是就是自己创建的目录节点，如果正是自己创建的，那么它就获得了这个锁，
    如果不是那么它就调用 exists(String path, boolean watch) 方法并监控 Zookeeper 上目录节点列表的变化，一直到自己创建的节点是列表中最小编号的目录节点，
    从而获得锁，释放锁很简单，只要删除前面它自己所创建的目录节点就行了。
  */
    public class ZooKeeprDistributedLockFromJava : IWatcher
    {
        private ZooKeeper zk;
        private string root = "/locks"; //根
        private string lockName; //竞争资源的标志
        private string waitNode; //等待前一个锁
        private string myZnode; //当前锁
        //private CountDownLatch latch; //计数器
        private AutoResetEvent autoevent;
        private TimeSpan sessionTimeout = TimeSpan.FromMilliseconds(30000);
        private IList<Exception> exception = new List<Exception>();

        /// <summary>
        /// 创建分布式锁,使用前请确认config配置的zookeeper服务可用 </summary>
        /// <param name="config"> 127.0.0.1:2181 </param>
        /// <param name="lockName"> 竞争资源标志,lockName中不能包含单词lock </param>
        public ZooKeeprDistributedLockFromJava(string config, string lockName)
        {
            this.lockName = lockName;
            // 创建一个与服务器的连接
            try
            {
                zk = new ZooKeeper(config, sessionTimeout, this);
                var stat = zk.Exists(root, false);
                if (stat == null)
                {
                    // 创建根节点
                    zk.Create(root, new byte[0], Ids.OPEN_ACL_UNSAFE, CreateMode.Persistent);
                }
            }
            catch (KeeperException e)
            {
                throw e;
            }
        }

        /// <summary>
        /// zookeeper节点的监视器
        /// </summary>
        public virtual void Process(WatchedEvent @event)
        {
            if (this.autoevent != null)
            {
                this.autoevent.Set();
            }
        }

        public virtual bool tryLock()
        {
            try
            {
                string splitStr = "_lock_";
                if (lockName.Contains(splitStr))
                {
                    //throw new LockException("lockName can not contains \\u000B");
                }
                //创建临时子节点
                myZnode = zk.Create(root + "/" + lockName + splitStr, new byte[0], Ids.OPEN_ACL_UNSAFE, CreateMode.EphemeralSequential);
                Console.WriteLine(myZnode + " is created ");
                //取出所有子节点
                IList<string> subNodes = zk.GetChildren(root, false);
                //取出所有lockName的锁
                IList<string> lockObjNodes = new List<string>();
                foreach (string node in subNodes)
                {
                    if (node.StartsWith(lockName))
                    {
                        lockObjNodes.Add(node);
                    }
                }
                Array alockObjNodes = lockObjNodes.ToArray();
                Array.Sort(alockObjNodes);
                Console.WriteLine(myZnode + "==" + lockObjNodes[0]);
                if (myZnode.Equals(root + "/" + lockObjNodes[0]))
                {
                    //如果是最小的节点,则表示取得锁
                    return true;
                }
                //如果不是最小的节点，找到比自己小1的节点
                string subMyZnode = myZnode.Substring(myZnode.LastIndexOf("/", StringComparison.Ordinal) + 1);
                waitNode = lockObjNodes[Array.BinarySearch(alockObjNodes, subMyZnode) - 1];
            }
            catch (KeeperException e)
            {
                throw e;
            }
            return false;
        }

        public virtual bool tryLock(TimeSpan time)
        {
            try
            {
                if (this.tryLock())
                {
                    return true;
                }
                return waitForLock(waitNode, time);
            }
            catch (KeeperException e)
            {
                throw e;
            }
            return false;
        }

        private bool waitForLock(string lower, TimeSpan waitTime)
        {
            var stat = zk.Exists(root + "/" + lower, true);
            //判断比自己小一个数的节点是否存在,如果不存在则无需等待锁,同时注册监听
            if (stat != null)
            {
                Console.WriteLine("Thread " + System.Threading.Thread.CurrentThread.Name + " waiting for " + root + "/" + lower);
                autoevent = new AutoResetEvent(false);
                bool r = autoevent.WaitOne(waitTime);
                autoevent.Dispose();
                autoevent = null;
                return r;
            }
            else
                return true;
        }

        public virtual void unlock()
        {
            try
            {
                Console.WriteLine("unlock " + myZnode);
                zk.Delete(myZnode, -1);
                myZnode = null;
                zk.Dispose();
            }
            catch (KeeperException e)
            {
                throw e;
            }
        }




    }
}
