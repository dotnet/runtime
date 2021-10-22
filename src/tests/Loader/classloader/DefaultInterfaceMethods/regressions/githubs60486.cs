using System;

namespace CallingEnviromentStackTraceFromDim
{
    public interface IPublisher<out TData>
    {
        event Action<TData> OnPublish;
    }

    public interface ISubscriber<T>
    {
        void OnReceiveSubscription(T data);

        void Subscribe(IPublisher<T> publisher)
        {
            InternalSubscribe(this, publisher);
        }

        void Unsubscribe(IPublisher<T> publisher)
        {
            InternalUnsubscribe(this, publisher);
        }

        protected static void InternalSubscribe(ISubscriber<T> subscriber, IPublisher<T> publisher)
        {
            publisher.OnPublish += subscriber.OnReceiveSubscription;
            Console.WriteLine(Environment.StackTrace.ToString());
        }

        protected static void InternalUnsubscribe(ISubscriber<T> subscriber, IPublisher<T> publisher)
        {
            publisher.OnPublish -= subscriber.OnReceiveSubscription;
            Console.WriteLine(Environment.StackTrace.ToString());
        }
    }

    public class PubTest :IPublisher<InputData>
    {
        public event Action<InputData> OnPublish;

        public void Call() => OnPublish?.Invoke(new InputData());
    }

    public class InputData
    {
        public int i;
    }

    public class Program : ISubscriber<InputData>
    {
        static int Main()
        {
            new Program().Start();
            return 100;
        }
        
        // Start is called before the first frame update
        public void Start()
        {
            var pub = new PubTest();
            var sub = (ISubscriber<InputData>)this;
            sub.Subscribe(pub);
            pub.Call();
            sub.Unsubscribe(pub);
        }

        public void Subscribe(IPublisher<InputData> publisher)
        {
            ISubscriber<InputData>.InternalSubscribe(this, publisher);
        }

        public void OnReceiveSubscription(InputData data)
        {
            Console.WriteLine($"do something");
        }
    }
}