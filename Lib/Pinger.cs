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

using System.Net.NetworkInformation;
using System.Text;
using System.Threading;

namespace Lib
{
    class Pinger
    {
        //https://msdn.microsoft.com/ru-ru/library/system.net.networkinformation.ping(v=vs.110).aspx

        /// <summary>
        /// Попытка пропинговать указанный хост с заданным числом попыток
        /// </summary>
        /// <param name="host">Хост для пинга</param>
        /// <param name="retries">Число попыток [4]</param>
        /// <param name="delay">Паузы между попытками [500 ms]</param>
        /// <returns>Успешность пинга указанного хоста</returns>
        public static bool TryPing(string host, int retries = 4, int delay = 500)
        {
            for (int i = 1; i <= retries; i++)
            {
                if (Ping(host))
                {
                    AppTrace.Verbose("Ping retries: {0}", i);
                    return true;
                }

                Thread.Sleep(delay);
            }

            AppTrace.Warning("Ping failed after {0} retries!", retries);
            return false;
        }

        /// <summary>
        /// Попытка пропинговать указанный хост
        /// </summary>
        /// <param name="host">Хост для пинга</param>
        /// <returns>Успешность пинга указанного хоста</returns>
        public static bool Ping(string host)
        {
            Pinged = false;

            AutoResetEvent waiter = new AutoResetEvent(false);
            Ping pingSender = new Ping();

            // When the PingCompleted event is raised,
            // the PingCompletedCallback method is called.
            pingSender.PingCompleted += new PingCompletedEventHandler(PingCompletedCallback);

            // Create a buffer of 32 bytes of data to be transmitted.
            string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            byte[] buffer = Encoding.ASCII.GetBytes(data);

            // Wait 12 seconds for a reply.
            int timeout = 12000;

            // Set options for transmission:
            // The data can go through 64 gateways or routers
            // before it is destroyed, and the data packet
            // cannot be fragmented.
            PingOptions options = new PingOptions(64, true);

            //Console.WriteLine("Time to live: {0}", options.Ttl);
            //Console.WriteLine("Don't fragment: {0}", options.DontFragment);

            // Send the ping asynchronously.
            // Use the waiter as the user token.
            // When the callback completes, it can wake up this thread.
            pingSender.SendAsync(host, timeout, buffer, options, waiter);

            // Prevent this application from ending.
            // A real application should do something useful
            // when possible.
            waiter.WaitOne();

            return Pinged;
        }

        public static bool Pinged = false;

        private static void PingCompletedCallback(object sender, PingCompletedEventArgs e)
        {
            // If the operation was canceled, display a message to the user.
            if (e.Cancelled)
            {
                //Console.WriteLine("Ping canceled.");

                // Let the main thread resume. 
                // UserToken is the AutoResetEvent object that the main thread 
                // is waiting for.
                ((AutoResetEvent)e.UserState).Set();
            }

            // If an error occurred, display the exception to the user.
            if (e.Error != null)
            {
                //Console.WriteLine("Ping failed:");
                //Console.WriteLine(e.Error.ToString());
                AppTrace.Warning("Ping failed: {0}", e.Error.ToString());

                // Let the main thread resume. 
                ((AutoResetEvent)e.UserState).Set();
            }

            PingReply reply = e.Reply;

            DisplayReply(reply);

            // Let the main thread resume.
            ((AutoResetEvent)e.UserState).Set();
        }

        public static void DisplayReply(PingReply reply)
        {
            if (reply == null)
            {
                return;
            }

            //Console.WriteLine("ping status: {0}", reply.Status);

            if (reply.Status == IPStatus.Success)
            {
                Pinged = true;
                //Console.WriteLine("Address: {0}", reply.Address.ToString());
                //Console.WriteLine("RoundTrip time: {0}", reply.RoundtripTime);
                //Console.WriteLine("Time to live: {0}", reply.Options.Ttl);
                //Console.WriteLine("Don't fragment: {0}", reply.Options.DontFragment);
                //Console.WriteLine("Buffer size: {0}", reply.Buffer.Length);
            }
        }
    }
}
