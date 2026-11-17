using System;
using System.Threading;
using System.Threading.Tasks;

namespace ZlhrsHelper {
    internal static class RequestDeadline {
        internal const int Seconds=60;
        // A transport can acknowledge cancellation late. Release the caller as soon as the deadline fires.
        internal static async Task<T> WaitAsync<T>(Task<T> work,CancellationToken token){
            var canceled=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using(token.Register(()=>canceled.TrySetResult(true))){
                var completed=await Task.WhenAny(work,canceled.Task).ConfigureAwait(false);
                if(completed==work)return await work.ConfigureAwait(false);
                work.ContinueWith(t=>GC.KeepAlive(t.Exception),CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted|TaskContinuationOptions.ExecuteSynchronously,TaskScheduler.Default);
                throw new OperationCanceledException(token);
            }
        }
    }
}
