using System;
using System.Collections.Generic;

namespace CommandTest.Models
{
    /// <summary>
    /// コマンド実行要求のキューを管理するクラス
    /// </summary>
    public class CommandQueue
    {
        private readonly Queue<CommandRequest> requestQueue = new Queue<CommandRequest>();
        private readonly object lockObject = new object();

        /// <summary>
        /// キューにコマンド実行要求を追加します
        /// </summary>
        /// <param name="request">コマンド実行要求</param>
        public void Enqueue(CommandRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            lock (lockObject)
            {
                requestQueue.Enqueue(request);
            }
        }

        /// <summary>
        /// キューから次のコマンド実行要求を取得します
        /// </summary>
        /// <returns>コマンド実行要求。キューが空の場合はnull</returns>
        public CommandRequest? TryDequeue()
        {
            lock (lockObject)
            {
                return requestQueue.Count > 0 ? requestQueue.Dequeue() : null;
            }
        }

        /// <summary>
        /// キューにコマンド実行要求が存在するかどうかを取得します
        /// </summary>
        public bool HasPendingRequests
        {
            get
            {
                lock (lockObject)
                {
                    return requestQueue.Count > 0;
                }
            }
        }

        /// <summary>
        /// キューをクリアします
        /// </summary>
        public void Clear()
        {
            lock (lockObject)
            {
                requestQueue.Clear();
            }
        }
    }
}
