#region License
//------------------------------------------------------------------------------
// Copyright (c) Dmitrii Evdokimov
// Source https://github.com/diev/
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//------------------------------------------------------------------------------
#endregion

using System;
using System.Threading;

namespace Lib
{
    public static class WaitKey
    {
        const int StartWait = 250; // 1/4 second
        const int NextWaits = 250; // 1/4 second

        public static bool Canceled = false;
        public static bool TimedOut = false;
        public static bool Pressed = false;
        public static ConsoleKeyInfo KeyInfo;

        static bool WaitEscapeOnly = false;

        public static void Timeout(bool EscToCancel = true, int TimeToCancel = 5000)
        {
            Reset();

            // Create an AutoResetEvent to signal the timeout threshold in the
            // timer callback has been reached.
            var autoEvent = new AutoResetEvent(false);
            var statusChecker = new StatusChecker(1 + (TimeToCancel - StartWait) / NextWaits);

            // Create a timer that invokes CheckStatus after ms and every ms thereafter.
            // Console.WriteLine("{0:h:mm:ss.fff} Creating timer.\n", DateTime.Now);
            var stateTimer = new Timer(statusChecker.CheckStatus, autoEvent, StartWait, NextWaits);

            //Console.WriteLine("Wait {0} sec or press Esc to cancel.", TimeToCancel / 1000);

            // When autoEvent signals time is out, dispose of the timer.
            autoEvent.WaitOne();
            stateTimer.Dispose();

            if (Canceled)
            {
                AppTrace.Information("Waiting canceled by user.");
            }

            else if (TimedOut)
            {
                AppTrace.Information("Waiting timed out.");
            }
        }

        public static bool Escape(int TimeToCancel = 5000)
        {
            Reset();
            WaitEscapeOnly = true;

            // Create an AutoResetEvent to signal the timeout threshold in the
            // timer callback has been reached.
            var autoEvent = new AutoResetEvent(false);
            var statusChecker = new StatusChecker(1 + (TimeToCancel - StartWait) / NextWaits);

            // Create a timer that invokes CheckStatus after ms and every ms thereafter.
            // Console.WriteLine("{0:h:mm:ss.fff} Creating timer.\n", DateTime.Now);
            var stateTimer = new Timer(statusChecker.CheckStatus, autoEvent, StartWait, NextWaits);

            //Console.WriteLine("Wait {0} sec or press Esc to cancel.", TimeToCancel / 1000);

            // When autoEvent signals time is out, dispose of the timer.
            autoEvent.WaitOne();
            stateTimer.Dispose();

            if (Canceled)
            {
                AppTrace.Information("Waiting canceled by user.");
            }

            else if (TimedOut)
            {
                AppTrace.Information("Waiting timed out.");
            }

            return Canceled;
        }

        private static void Reset()
        {
            Canceled = false;
            TimedOut = false;
            Pressed = false;
            WaitEscapeOnly = false;

            AppTrace.Information("Waiting a key...");
        }

        class StatusChecker
        {
            private int invokeCount;
            private readonly int maxCount;

            public StatusChecker(int count)
            {
                invokeCount = 0;
                maxCount = count;
            }

            // This method is called by the timer delegate.
            public void CheckStatus(Object stateInfo)
            {
                AutoResetEvent autoEvent = (AutoResetEvent)stateInfo;
                invokeCount++;
                // Console.WriteLine("{0:h:mm:ss.fff} Checking status {1,2}.", DateTime.Now, invokeCount);

                if (Console.KeyAvailable)
                {
                    KeyInfo = new ConsoleKeyInfo();
                    KeyInfo = Console.ReadKey(true);

                    if (WaitEscapeOnly)
                    {
                        if (KeyInfo.Key == ConsoleKey.Escape)
                        {
                            invokeCount = 0;
                            Canceled = true;
                            Pressed = true;
                        }
                    }

                    else
                    {
                        invokeCount = 0;
                        Canceled = (KeyInfo.Key == ConsoleKey.Escape);
                        Pressed = true;
                    }
                }

                else if (invokeCount == maxCount)
                {
                    invokeCount = 0;
                    TimedOut = true;
                }

                if (invokeCount == 0)
                {
                    // Signal the waiting thread.
                    autoEvent.Set();
                }
            }
        }
    }
}
