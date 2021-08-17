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

#define TRACE
#define ConfigFile

using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace Lib
{
    /// <summary>
    /// Helper class replaces Trace
    /// </summary>
    public static class AppTrace
    {
        #region Public properties

        public static TraceSource TraceSource { get; private set; }

        public static int WarningCount { get; private set; } = 0;
        public static int ErrorCount { get; private set; } = 0;

        #endregion Public properties

        #region Constructors

        static AppTrace()
        {
             TraceSource = new TraceSource(Assembly.GetCallingAssembly().GetName().Name, SourceLevels.All);
        }

        #endregion Constructors

        #region Public methods

        public static void Close()
        {
            TraceSource.Close();
        }

        public static void Error(string message)
        {
            HelperTrace(TraceEventType.Error, message);
        }

        public static void Error(StringBuilder message)
        {
            HelperTrace(TraceEventType.Error, message.ToString());
        }

        public static void Error(string format, params object[] values)
        {
            HelperTrace(TraceEventType.Error, format, values);
        }

        public static void ErrorIf(bool condition, string message)
        {
            HelperTraceIf(TraceEventType.Error, condition, message);
        }

        public static void ErrorIf(bool condition, StringBuilder message)
        {
            HelperTraceIf(TraceEventType.Error, condition, message.ToString());
        }

        public static void ErrorIf(bool condition, string format, params object[] values)
        {
            HelperTraceIf(TraceEventType.Error, condition, format, values);
        }

        public static void Information(string message)
        {
            HelperTrace(TraceEventType.Information, message);
        }

        public static void Information(StringBuilder message)
        {
            HelperTrace(TraceEventType.Information, message.ToString());
        }

        public static void Information(string format, params object[] values)
        {
            HelperTrace(TraceEventType.Information, format, values);
        }

        public static void InformationIf(bool condition, string message)
        {
            HelperTraceIf(TraceEventType.Information, condition, message);
        }

        public static void InformationIf(bool condition, StringBuilder message)
        {
            HelperTraceIf(TraceEventType.Information, condition, message.ToString());
        }

        public static void InformationIf(bool condition, string format, params object[] values)
        {
            HelperTraceIf(TraceEventType.Information, condition, format, values);
        }

        public static void Verbose(string message)
        {
            HelperTrace(TraceEventType.Verbose, message);
        }

        public static void Verbose(StringBuilder message)
        {
            HelperTrace(TraceEventType.Verbose, message.ToString());
        }

        public static void Verbose(string format, params object[] values)
        {
            HelperTrace(TraceEventType.Verbose, format, values);
        }

        public static void VerboseIf(bool condition, string message)
        {
            HelperTraceIf(TraceEventType.Verbose, condition, message);
        }

        public static void VerboseIf(bool condition, StringBuilder message)
        {
            HelperTraceIf(TraceEventType.Verbose, condition, message.ToString());
        }

        public static void VerboseIf(bool condition, string format, params object[] values)
        {
            HelperTraceIf(TraceEventType.Verbose, condition, format, values);
        }

        public static void Warning(string message)
        {
            HelperTrace(TraceEventType.Warning, message);
        }

        public static void Warning(StringBuilder message)
        {
            HelperTrace(TraceEventType.Warning, message.ToString());
        }

        public static void Warning(string format, params object[] values)
        {
            HelperTrace(TraceEventType.Warning, format, values);
        }

        public static void WarningIf(bool condition, string message)
        {
            HelperTraceIf(TraceEventType.Warning, condition, message);
        }

        public static void WarningIf(bool condition, StringBuilder message)
        {
            HelperTraceIf(TraceEventType.Warning, condition, message.ToString());
        }

        public static void WarningIf(bool condition, string format, params object[] values)
        {
            HelperTraceIf(TraceEventType.Warning, condition, format, values);
        }

        #endregion Public methods

        #region Private methods

        private static void HelperTrace(TraceEventType category, string format, params object[] values)
        {
            HelperTrace(category, string.Format(format, values));
        }

        private static void HelperTraceIf(TraceEventType category, bool condition, string format, params object[] values)
        {
            HelperTraceIf(category, condition, string.Format(format, values));
        }

        private static void HelperTraceIf(TraceEventType category, bool condition, string message)
        {
            if (condition)
            {
                if (category == TraceEventType.Error)
                {
                    ErrorCount++;
                }
                else if (category == TraceEventType.Warning)
                {
                    WarningCount++;
                }

                TraceSource.TraceEvent(category, 0, message);
            }
        }

        private static void HelperTrace(TraceEventType category, string message)
        {
            HelperTraceIf(category, true, message);
        }

        #endregion Private methods
    }
}
