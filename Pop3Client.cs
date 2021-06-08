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

// based on an article https://www.codeproject.com/Articles/14304/POP3-Email-Client-NET-2-0

// POP3 Client
// ===========
//
// copyright by Peter Huber, Singapore, 2006
// this code is provided as is, bugs are probable, free for any use, no responsibility accepted :-)
//
// based on POP3 Client as a C# Class, by Bill Dean, http://www.codeproject.com/csharp/Pop3MailClient.asp 
// based on Retrieve Mail From a POP3 Server Using C#, by Agus Kurniawan, http://www.codeproject.com/csharp/popapp.asp 
// based on Post Office Protocol - Version 3, http://www.ietf.org/rfc/rfc1939.txt
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;

namespace Pop3
{
    // Supporting classes and structs
    // ==============================

    /// <summary>
    /// Combines Email ID with Email UID for one email
    /// The POP3 server assigns to each message a unique Email UID, which will not change for the life time
    /// of the message and no other message should use the same.
    /// 
    /// Exceptions:
    /// Throws Pop3Exception if there is a serious communication problem with the POP3 server, otherwise
    /// 
    /// </summary>
    public struct EmailUid
    {
        /// <summary>
        /// used in POP3 commands to indicate which message (only valid in the present session)
        /// </summary>
        public int EmailId;

        /// <summary>
        /// Uid is always the same for a message, regardless of session
        /// </summary>
        public string Uid;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="emailId"></param>
        /// <param name="uid"></param>
        public EmailUid(int emailId, string uid)
        {
            EmailId = emailId;
            Uid = uid;
        }
    }

    /// <summary>
    /// If anything goes wrong within Pop3MailClient, a Pop3Exception is raised
    /// </summary>
    public class Pop3Exception : ApplicationException
    {
        /// <summary>
        /// Pop3 exception with no further explanation
        /// </summary>
        public Pop3Exception() { }

        /// <summary>
        /// Pop3 exception with further explanation
        /// </summary>
        /// <param name="ErrorMessage"></param>
        public Pop3Exception(string ErrorMessage) : base(ErrorMessage) { }
    }

    /// <summary>
    /// A pop3 connection goes through the following states:
    /// </summary>
    public enum Pop3ConnectionStateEnum
    {
        /// <summary>
        /// undefined
        /// </summary>
        None = 0,

        /// <summary>
        /// not connected yet to POP3 server
        /// </summary>
        Disconnected,

        /// <summary>
        /// TCP connection has been opened and the POP3 server has sent the greeting. POP3 server expects user name and password
        /// </summary>
        Authorization,

        /// <summary>
        /// client has identified itself successfully with the POP3, server has locked all messages 
        /// </summary>
        Connected,

        /// <summary>
        /// QUIT command was sent, the server has deleted messages marked for deletion and released the resources
        /// </summary>
        Closed
    }

    // Delegates for Pop3Client
    // ========================

    /// <summary>
    /// If POP3 Server doesn't react as expected or this code has a problem, but
    /// can continue with the execution, a Warning is called.
    /// </summary>
    /// <param name="WarningText"></param>
    /// <param name="Response">string received from POP3 server</param>
    public delegate void WarningHandler(string WarningText, string Response);

    /// <summary>
    /// Traces all the information exchanged between POP3 client and POP3 server plus some
    /// status messages from POP3 client.
    /// Helpful to investigate any problem.
    /// Console.WriteLine() can be used
    /// </summary>
    /// <param name="TraceText"></param>
    public delegate void TraceHandler(string TraceText);

    // Pop3Client Class
    // ================  

    /// <summary>
    /// provides access to emails on a POP3 Server
    /// </summary>
    public class Pop3Client
    {
        #region Events
        // Events
        // ------

        /// <summary>
        /// Called whenever POP3 server doesn't react as expected, but no runtime error is thrown.
        /// </summary>
        public event WarningHandler Warning;

        /// <summary>
        /// call warning event
        /// </summary>
        /// <param name="methodName">name of the method where warning is needed</param>
        /// <param name="response">answer from POP3 server causing the warning</param>
        /// <param name="warningText">explanation what went wrong</param>
        /// <param name="warningParameters"></param>
        protected void CallWarning(string methodName, string response, string warningText, params object[] warningParameters)
        {
            warningText = string.Format(warningText, warningParameters);
            Warning?.Invoke($"{methodName}: {warningText}", response);
            CallTrace($"!! {warningText}");
        }

        /// <summary>
        /// Shows the communication between PopClient and PopServer, including warnings
        /// </summary>
        public event TraceHandler Trace;

        /// <summary>
        /// call Trace event
        /// </summary>
        /// <param name="text">string to be traced</param>
        protected void CallTrace(string text)
        {
            Trace?.Invoke($"{DateTime.Now:HH:mm:ss} {text}");
        }
        #endregion Events

        #region Properties
        // Properties
        // ----------

        /// <summary>
        /// Get POP3 server name
        /// </summary>
        public string PopServer { get; }

        /// <summary>
        /// Get POP3 server port
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// Should SSL be used for connection with POP3 server?
        /// </summary>
        public bool UseSSL { get; }

        /// <summary>
        /// should Pop3MailClient automatically reconnect if POP3 server has dropped the 
        /// connection due to a timeout?
        /// </summary>
        public bool IsAutoReconnect { get; set; } = false;

        //timeout has occured, we try to perform an autoreconnect
        private bool _isTimeoutReconnect = false;

        /// <summary>
        /// Get / set read timeout (miliseconds)
        /// </summary>
        public int ReadTimeout
        {
            get => _readTimeout;
            set
            {
                _readTimeout = value;

                if (_pop3Stream != null && _pop3Stream.CanTimeout)
                {
                    _pop3Stream.ReadTimeout = _readTimeout;
                }
            }
        }

        /// <summary>
        /// POP3 server read timeout
        /// </summary>
        protected int _readTimeout = -1;

        /// <summary>
        /// Get owner name of mailbox on POP3 server
        /// </summary>
        public string Username { get; }

        /// <summary>
        /// Get password for mailbox on POP3 server
        /// </summary>
        public string Password { get; }

        /// <summary>
        /// Get connection status with POP3 server
        /// </summary>
        public Pop3ConnectionStateEnum Pop3ConnectionState { get; protected set; } = Pop3ConnectionStateEnum.Disconnected;
        #endregion Properties

        #region Methods
        // Methods
        // -------

        /// <summary>
        /// Set POP3 connection state
        /// </summary>
        /// <param name="State"></param>
        protected void SetPop3ConnectionState(Pop3ConnectionStateEnum State)
        {
            Pop3ConnectionState = State;
            CallTrace($"  Connection State: {State}");
        }

        /// <summary>
        /// throw exception if POP3 connection is not in the required state
        /// </summary>
        /// <param name="requiredState"></param>
        protected void EnsureState(Pop3ConnectionStateEnum requiredState)
        {
            if (Pop3ConnectionState != requiredState)
            {
                // wrong connection state
                throw new Pop3Exception($"GetMailboxStats only accepted during connection state: {requiredState}" +
                      $"\n The connection to server {PopServer} is in state {Pop3ConnectionState}");
            }
        }
        #endregion Methods

        #region Private fields
        // Private fields
        // --------------

        /// <summary>
        /// TCP to POP3 server
        /// </summary>
        private TcpClient _serverTcpConnection;

        /// <summary>
        /// Stream from POP3 server with or without SSL
        /// </summary>
        private Stream _pop3Stream;

        /// <summary>
        /// Reader for POP3 message
        /// </summary>
        protected StreamReader _pop3StreamReader;

        /// <summary>
        /// char 'array' for carriage return / line feed
        /// </summary>
        protected const string CRLF = "\r\n";
        #endregion Private fields

        #region Public methods
        // Public methods
        // --------------

        /// <summary>
        /// Make POP3 client ready to connect to POP3 server
        /// </summary>
        /// <param name="popServer"><example>pop.gmail.com</example></param>
        /// <param name="port"><example>995</example></param>
        /// <param name="useSSL">True: SSL is used for connection to POP3 server</param>
        /// <param name="username"><example>abc@gmail.com</example></param>
        /// <param name="password">Secret</param>
        public Pop3Client(string popServer, int port, bool useSSL, string username, string password)
        {
            PopServer = popServer;
            Port = port;
            UseSSL = useSSL;
            Username = username;
            Password = password;
        }

        /// <summary>
        /// Connect to POP3 server
        /// </summary>
        public void Connect()
        {
            if (Pop3ConnectionState != Pop3ConnectionStateEnum.Disconnected &&
              Pop3ConnectionState != Pop3ConnectionStateEnum.Closed &&
              !_isTimeoutReconnect)
            {
                CallWarning("connect", "", $"Connect command received, but state is: {Pop3ConnectionState}");
            }

            else
            {
                //establish TCP connection
                try
                {
                    CallTrace($"  Connect to {PopServer}:{Port}");
                    _serverTcpConnection = new TcpClient(PopServer, Port);
                }
                catch (Exception ex)
                {
                    throw new Pop3Exception($"Connection to server {PopServer}, port {Port} failed.\nRuntime Error: {ex}");
                }

                if (UseSSL)
                {
                    //get SSL stream
                    try
                    {
                        CallTrace("  Get SSL connection");
                        _pop3Stream = new SslStream(_serverTcpConnection.GetStream(), false);
                        _pop3Stream.ReadTimeout = _readTimeout;
                    }
                    catch (Exception ex)
                    {
                        throw new Pop3Exception($"Server {PopServer} found, but cannot get SSL data stream.\nRuntime Error: {ex}");
                    }

                    //perform SSL authentication
                    try
                    {
                        CallTrace("  Get SSL authentication");
                        ((SslStream)_pop3Stream).AuthenticateAsClient(PopServer);
                    }
                    catch (Exception ex)
                    {
                        throw new Pop3Exception($"Server {PopServer} found, but problem with SSL Authentication.\nRuntime Error: {ex}");
                    }
                }

                else
                {
                    //create a stream to POP3 server without using SSL
                    try
                    {
                        CallTrace("  Get connection without SSL");
                        _pop3Stream = _serverTcpConnection.GetStream();
                        _pop3Stream.ReadTimeout = _readTimeout;
                    }
                    catch (Exception ex)
                    {
                        throw new Pop3Exception($"Server {PopServer} found, but cannot get data stream (without SSL).\nRuntime Error: {ex}");
                    }
                }

                //get stream for reading from pop server
                //POP3 allows only US-ASCII. The message will be translated in the proper encoding in a later step
                try
                {
                    _pop3StreamReader = new StreamReader(_pop3Stream, Encoding.ASCII);
                }
                catch (Exception ex)
                {
                    throw new Pop3Exception(UseSSL ?
                        $"Server {PopServer} found, but cannot read from SSL stream.\nRuntime Error: {ex}" :
                        $"Server {PopServer} found, but cannot read from stream (without SSL).\nRuntime Error: {ex}");
                }

                //ready for authorisation
                if (!ReadSingleLine(out string response))
                {
                    throw new Pop3Exception($"Server {PopServer} not ready to start AUTHORIZATION.\nMessage: {response}");
                }

                SetPop3ConnectionState(Pop3ConnectionStateEnum.Authorization);

                //send user name
                if (!ExecuteCommand($"USER {Username}", out response))
                {
                    throw new Pop3Exception($"Server {PopServer} doesn't accept username '{Username}'.\nMessage: {response}");
                }

                //send password
                if (!ExecuteCommand($"PASS {Password}", out response))
                {
                    throw new Pop3Exception($"Server {PopServer} doesn't accept password '{Password}' for user '{Username}'.\nMessage: {response}");
                }

                SetPop3ConnectionState(Pop3ConnectionStateEnum.Connected);
            }
        }

        /// <summary>
        /// Disconnect from POP3 Server
        /// </summary>
        public void Disconnect()
        {
            if (Pop3ConnectionState == Pop3ConnectionStateEnum.Disconnected ||
              Pop3ConnectionState == Pop3ConnectionStateEnum.Closed)
            {
                CallWarning("Disconnect", "", "Disconnect received, but was already disconnected.");
            }

            else
            {
                //ask server to end session and possibly to remove emails marked for deletion
                try
                {
                    if (ExecuteCommand("QUIT", out string response))
                    {
                        //server says everything is ok
                        SetPop3ConnectionState(Pop3ConnectionStateEnum.Closed);
                    }

                    else
                    {
                        //server says there is a problem
                        CallWarning("Disconnect", response, $"Negative response from server while closing connection: {response}");
                        SetPop3ConnectionState(Pop3ConnectionStateEnum.Disconnected);
                    }
                }
                finally
                {
                    //close connection
                    if (_pop3Stream != null)
                    {
                        _pop3Stream.Close();
                    }

                    _pop3StreamReader.Close();
                }
            }
        }

        /// <summary>
        /// Delete message from server.
        /// The POP3 server marks the message as deleted.  Any future
        /// reference to the message-number associated with the message
        /// in a POP3 command generates an error.  The POP3 server does
        /// not actually delete the message until the POP3 session
        /// enters the UPDATE state.
        /// </summary>
        /// <param name="msg_number"></param>
        /// <returns></returns>
        public bool DeleteEmail(int msg_number)
        {
            EnsureState(Pop3ConnectionStateEnum.Connected);

            if (!ExecuteCommand($"DELE {msg_number}", out string response))
            {
                CallWarning("DeleteEmail", response, $"Negative response for email (Id: {msg_number}) delete request");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Get a list of all Email IDs available in mailbox
        /// </summary>
        /// <returns></returns>
        public bool GetEmailIdList(out List<int> EmailIds)
        {
            EnsureState(Pop3ConnectionStateEnum.Connected);
            EmailIds = new List<int>();

            //get server response status line
            if (!ExecuteCommand("LIST", out string response))
            {
                CallWarning("GetEmailIdList", response, "Negative response for email list request");
                return false;
            }

            //get every email id
            while (ReadMultiLine(out response))
            {
                if (int.TryParse(response.Split(' ')[0], out int EmailId))
                {
                    EmailIds.Add(EmailId);
                }

                else
                {
                    CallWarning("GetEmailIdList", response, "first characters should be integer (EmailId)");
                }
            }

            CallTrace($"  {EmailIds.Count} email ids received");

            return true;
        }

        /// <summary>
        /// get size of one particular email
        /// </summary>
        /// <param name="msg_number"></param>
        /// <returns></returns>
        public int GetEmailSize(int msg_number)
        {
            EnsureState(Pop3ConnectionStateEnum.Connected);
            ExecuteCommand($"LIST {msg_number}", out string response);

            int EmailSize = 0;
            string[] responseSplit = response.Split(' ');

            if (responseSplit.Length < 2 || !int.TryParse(responseSplit[2], out EmailSize))
            {
                CallWarning("GetEmailSize", response, "'+OK int int' format expected (EmailId, EmailSize)");
            }

            return EmailSize;
        }

        /// <summary>
        /// Get a list with the unique IDs of all Email available in mailbox.
        /// 
        /// Explanation:
        /// EmailIds for the same email can change between sessions, whereas the unique Email id
        /// never changes for an email.
        /// </summary>
        /// <param name="EmailIds"></param>
        /// <returns></returns>
        public bool GetUniqueEmailIdList(out List<EmailUid> EmailIds)
        {
            EnsureState(Pop3ConnectionStateEnum.Connected);
            EmailIds = new List<EmailUid>();

            //get server response status line
            if (!ExecuteCommand("UIDL", out string response))
            {
                CallWarning("GetUniqueEmailIdList", response, "Negative response for email list request");
                return false;
            }

            //get every email unique id
            while (ReadMultiLine(out response))
            {
                string[] responseSplit = response.Split(' ');

                if (responseSplit.Length < 2)
                {
                    CallWarning("GetUniqueEmailIdList", response, "Response not in format 'int string'");
                }

                else if (!int.TryParse(responseSplit[0], out int EmailId))
                {
                    CallWarning("GetUniqueEmailIdList", response, "First charaters should be integer (Unique EmailId)");
                }

                else
                {
                    EmailIds.Add(new EmailUid(EmailId, responseSplit[1]));
                }
            }

            CallTrace($"  {EmailIds.Count} unique email ids received");

            return true;
        }

        /// <summary>
        /// get a list with all currently available messages and the UIDs
        /// </summary>
        /// <param name="EmailIds">EmailId Uid list</param>
        /// <returns>false: server sent negative response (didn't send list)</returns>
        public bool GetUniqueEmailIdList(out SortedList<string, int> EmailIds)
        {
            EnsureState(Pop3ConnectionStateEnum.Connected);
            EmailIds = new SortedList<string, int>();

            //get server response status line
            if (!ExecuteCommand("UIDL", out string response))
            {
                CallWarning("GetUniqueEmailIdList", response, "Negative response for email list request");
                return false;
            }

            //get every email unique id
            while (ReadMultiLine(out response))
            {
                string[] responseSplit = response.Split(' ');

                if (responseSplit.Length < 2)
                {
                    CallWarning("GetUniqueEmailIdList", response, "Response not in format 'int string'");
                }

                else if (!int.TryParse(responseSplit[0], out int EmailId))
                {
                    CallWarning("GetUniqueEmailIdList", response, "First charaters should be integer (Unique EmailId)");
                }

                else
                {
                    EmailIds.Add(responseSplit[1], EmailId);
                }
            }

            CallTrace($"  {EmailIds.Count} unique email ids received");

            return true;
        }

        /// <summary>
        /// Get size of one particular email
        /// </summary>
        /// <param name="msg_number"></param>
        /// <returns></returns>
        public int GetUniqueEmailId(EmailUid msg_number)
        {
            EnsureState(Pop3ConnectionStateEnum.Connected);
            ExecuteCommand($"LIST {msg_number}", out string response);

            int EmailSize = 0;
            string[] responseSplit = response.Split(' ');

            if (responseSplit.Length < 2 || !int.TryParse(responseSplit[2], out EmailSize))
            {
                CallWarning("GetEmailSize", response, "'+OK int int' format expected (EmailId, EmailSize)");
            }

            return EmailSize;
        }

        /// <summary>
        /// Sends an 'empty' command to the POP3 server. Server has to respond with +OK
        /// </summary>
        /// <returns>true: server responds as expected</returns>
        public bool NOOP()
        {
            EnsureState(Pop3ConnectionStateEnum.Connected);

            if (!ExecuteCommand("NOOP", out string response))
            {
                CallWarning("NOOP", response, "Negative response for NOOP request");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Should the raw content, the US-ASCII code as received, be traced
        /// GetRawEmail will switch it on when it starts and off once finished
        /// 
        /// Inheritors might use it to get the raw email
        /// </summary>
        protected bool _isTraceRawEmail = false;

        /// <summary>
        /// Contains one MIME part of the email in US-ASCII, needs to be translated in .NET string (Unicode).
        /// Contains the complete email in US-ASCII, needs to be translated in .NET string (Unicode).
        /// For speed reasons, reuse StringBuilder.
        /// </summary>
        protected StringBuilder _rawEmailSB;

        /// <summary>
        /// Reads the complete text of a message
        /// </summary>
        /// <param name="messageNo">Email to retrieve</param>
        /// <param name="emailText">ASCII string of complete message</param>
        /// <returns></returns>
        public bool GetRawEmail(int messageNo, out string emailText)
        {
            //send 'RETR int' command to server
            if (!SendRetrCommand(messageNo))
            {
                emailText = null;
                return false;
            }

            //get the lines
            int lineCounter = 0;

            //empty StringBuilder
            if (_rawEmailSB == null)
            {
                _rawEmailSB = new StringBuilder(100000);
            }

            else
            {
                _rawEmailSB.Length = 0;
            }

            _isTraceRawEmail = true;

            while (ReadMultiLine(out _))
            {
                lineCounter++;
            }

            emailText = _rawEmailSB.ToString();
            CallTrace($"  Email with {lineCounter} lines, {emailText.Length} chars received");

            return true;
        }

        /// <summary>
        /// Unmark any emails from deletion. The server only deletes email really
        /// once the connection is properly closed.
        /// </summary>
        /// <returns>true: emails are unmarked from deletion</returns>
        public bool UndeleteAllEmails()
        {
            EnsureState(Pop3ConnectionStateEnum.Connected);

            return ExecuteCommand("RSET", out _);
        }

        /// <summary>
        /// Get mailbox statistics
        /// </summary>
        /// <param name="numberOfMails"></param>
        /// <param name="mailboxSize"></param>
        /// <returns></returns>
        public bool GetMailboxStats(out int numberOfMails, out int mailboxSize)
        {
            EnsureState(Pop3ConnectionStateEnum.Connected);

            if (ExecuteCommand("STAT", out string response))
            {
                //got a positive response
                string[] responseParts = response.Split(' ');

                if (responseParts.Length < 2)
                {
                    //response format wrong
                    throw new Pop3Exception($"Server {PopServer} sends illegally formatted response." +
                      "\nExpected format: +OK int int" +
                      "\nReceived response: " + response);
                }

                numberOfMails = int.Parse(responseParts[1]);
                mailboxSize = int.Parse(responseParts[2]);

                return true;
            }

            numberOfMails = 0;
            mailboxSize = 0;

            return false;
        }

        /// <summary>
        /// Send RETR command to POP 3 server to fetch one particular message
        /// </summary>
        /// <param name="messageNo">ID of message required</param>
        /// <returns>false: negative server respond, message not delivered</returns>
        protected bool SendRetrCommand(int messageNo)
        {
            EnsureState(Pop3ConnectionStateEnum.Connected);

            // retrieve mail with message number
            if (!ExecuteCommand($"RETR {messageNo}", out string response))
            {
                CallWarning("GetRawEmail", response, $"Negative response for email (ID: {messageNo}) request");
                return false;
            }

            return true;
        }
        #endregion Public methods

        #region Helper methods
        // Helper methods
        // --------------

        /// <summary>
        /// sends the 4 letter command to POP3 server (adds CRLF) and waits for the
        /// response of the server
        /// </summary>
        /// <param name="command">command to be sent to server</param>
        /// <param name="response">answer from server</param>
        /// <returns>false: server sent negative acknowledge, i.e. server could not execute command</returns>

        public bool isDebug = false;
        private bool ExecuteCommand(string command, out string response)
        {
            //send command to server
            byte[] commandBytes = Encoding.ASCII.GetBytes((command + CRLF).ToCharArray());

            if (command.StartsWith("PASS "))
            {
                CallTrace($"> {command.Replace(Password, "******")}");
            }

            else
            {
                CallTrace($"> {command}");
            }

            bool isSupressThrow;
            try
            {
                _pop3Stream.Write(commandBytes, 0, commandBytes.Length);

                if (isDebug)
                {
                    isDebug = false;
                    throw new IOException("Test", new SocketException(10053));
                }
            }
            catch (IOException ex)
            {
                //Unable to write data to the transport connection. Check if reconnection should be tried
                isSupressThrow = ExecuteReconnect(ex, command, commandBytes);

                if (!isSupressThrow)
                {
                    throw;
                }
            }

            _pop3Stream.Flush();

            //read response from server
            try
            {
                response = _pop3StreamReader.ReadLine();
            }
            catch (IOException ex)
            {
                //Unable to write data to the transport connection. Check if reconnection should be tried
                isSupressThrow = ExecuteReconnect(ex, command, commandBytes);

                if (isSupressThrow)
                {
                    //wait for response one more time
                    response = _pop3StreamReader.ReadLine();
                }

                else
                {
                    throw;
                }
            }

            if (response == null)
            {
                throw new Pop3Exception($"Server {PopServer} has not responded, timeout has occured.");
            }

            CallTrace($"< {response}");

            return (response.Length > 0 && response[0] == '+');
        }

        /// <summary>
        /// reconnect, if there is a timeout exception and isAutoReconnect is true
        /// </summary>
        private bool ExecuteReconnect(IOException ex, string command, byte[] commandBytes)
        {
            if (ex.InnerException != null && ex.InnerException is SocketException exception)
            {
                //SocketException
                SocketException innerEx = exception;

                if (innerEx.ErrorCode == 10053)
                {
                    //probably timeout: An established connection was aborted by the software in your host machine.
                    CallWarning("ExecuteCommand", "", "Probably timeout occured");

                    if (IsAutoReconnect)
                    {
                        //try to reconnect and send one more time
                        _isTimeoutReconnect = true;

                        try
                        {
                            CallTrace("  Try to auto reconnect");
                            Connect();

                            CallTrace("  Reconnect successful, try to resend command");
                            CallTrace($"> {command}");

                            _pop3Stream.Write(commandBytes, 0, commandBytes.Length);
                            _pop3Stream.Flush();

                            return true;
                        }
                        finally
                        {
                            _isTimeoutReconnect = false;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// read single line response from POP3 server. 
        /// <example>Example server response: +OK asdfkjahsf</example>
        /// </summary>
        /// <param name="response">response from POP3 server</param>
        /// <returns>true: positive response</returns>
        protected bool ReadSingleLine(out string response)
        {
            response = null;

            try
            {
                response = _pop3StreamReader.ReadLine();
            }
            catch (Exception ex)
            {
                _ = ex.Message;
            }

            if (response == null)
            {
                throw new Pop3Exception($"Server {PopServer} has not responded, timeout has occured.");
            }

            CallTrace($"< {response}");

            return (response.Length > 0 && response[0] == '+');
        }

        /// <summary>
        /// read one line in multiline mode from the POP3 server. 
        /// </summary>
        /// <param name="response">line received</param>
        /// <returns>false: end of message</returns>
        protected bool ReadMultiLine(out string response)
        {
            response = _pop3StreamReader.ReadLine();

            if (response == null)
            {
                throw new Pop3Exception($"Server {PopServer} has not responded, probably timeout has occured.");
            }

            if (_isTraceRawEmail)
            {
                //collect all responses as received
                _ = _rawEmailSB.Append(response + CRLF);
            }

            //check for byte stuffing, i.e. if a line starts with a '.', another '.' is added, unless
            //it is the last line
            if (response.Length > 0 && response[0] == '.')
            {
                if (response == ".")
                {
                    //closing line found
                    return false;
                }

                //remove the first '.'
                response = response.Substring(1);
            }

            return true;
        }
        #endregion Helper methods
    }
}
