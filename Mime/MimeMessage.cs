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

// based on an article https://www.codeproject.com/Articles/15611/POP3-Email-Client-with-full-MIME-Support-NET-2-0

// POP3 Email Client
// =================
//
// copyright by Peter Huber, Singapore, 2006
// this code is provided as is, bugs are probable, free for any use at own risk, no 
// responsibility accepted. All rights, title and interest in and to the accompanying content retained.  :-)
//
// based on Standard for ARPA Internet Text Messages, http://rfc.net/rfc822.html
// based on MIME Standard,  Internet Message Bodies, http://rfc.net/rfc2045.html
// based on MIME Standard, Media Types, http://rfc.net/rfc2046.html
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;

namespace Mime
{
    /// <summary>
    /// Stores all MIME decoded information of a received email. One email might consist of
    /// several MIME entities, which have a very similar structure to an email. A MimeMessage
    /// can be a top most level email or a MIME entity the emails contains.
    /// 
    /// According to various RFCs, MIME entities can contain other MIME entities 
    /// recursively. However, they usually need to be mapped to alternative views and 
    /// attachments, which are non recursive.
    ///
    /// MimeMessage inherits from System.Net.MailMessage, but provides additional receiving related information 
    /// </summary>
    public class MimeMessage : MailMessage
    {
        #region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
        public MimeMessage()
        {
            //for the moment, we assume to be at the top
            //should this entity become a child, TopParent will be overwritten
            TopParent = this;
            Entities = new List<MimeMessage>();
            UnknowHeaderlines = new List<string>();
        }

        #endregion Constructors

        #region Public properties

        /// <summary>
        /// To whom the email was delivered to
        /// </summary>
        public MailAddress DeliveredTo { get; set; }

        /// <summary>
        /// To whom the email was
        /// </summary>
        public MailAddress ReturnPath { get; set; }

        /// <summary>
        /// Date when the email was received
        /// </summary>
        public DateTime DeliveryDate { get; set; }

        /// <summary>
        /// ID @ server
        /// </summary>
        public string MessageId { get; set; }

        /// <summary>
        /// Probably '1.0'
        /// </summary>
        public string MimeVersion { get; set; }

        /// <summary>
        /// It may be desirable to allow one body to make reference to another. Accordingly, 
        /// bodies may be labelled using the "Content-ID" header field.    
        /// </summary>
        public string ContentId { get; set; }

        /// <summary>
        /// Some descriptive information for body
        /// </summary>
        public string ContentDescription { get; set; }

        /// <summary>
        /// ContentDisposition contains normally redundant information also stored in the 
        /// ContentType. Since ContentType is more detailed, it is enough to analyze ContentType
        /// 
        /// Something like:
        /// inline
        /// inline; filename="image001.gif
        /// attachment; filename="image001.jpg"
        /// </summary>
        public ContentDisposition ContentDisposition { get; set; }

        /// <summary>
        /// Something like "7bit" / "8bit" / "binary" / "quoted-printable" / "base64"
        /// </summary>
        public string TransferType { get; set; }

        /// <summary>
        /// Similar as TransferType, but .NET supports only "7bit" / "quoted-printable"
        /// / "base64" here, "bit8" is marked as "bit7" (i.e. no transfer encoding needed), 
        /// "binary" is illegal in SMTP
        /// </summary>
        public TransferEncoding ContentTransferEncoding { get; set; }

        /// <summary>
        /// The Content-Type field is used to specify the nature of the data in the body of a
        /// MIME entity, by giving media type and subtype identifiers, and by providing 
        /// auxiliary information that may be required for certain media types. Examples:
        /// text/plain;
        /// text/plain; charset=ISO-8859-1
        /// text/plain; charset=us-ascii
        /// text/plain; charset=utf-8
        /// text/html;
        /// text/html; charset=ISO-8859-1
        /// image/gif; name=image004.gif
        /// image/jpeg; name="image005.jpg"
        /// message/delivery-status
        /// message/rfc822
        /// multipart/alternative; boundary="----=_Part_4088_29304219.1115463798628"
        /// multipart/related; 	boundary="----=_Part_2067_9241611.1139322711488"
        /// multipart/mixed; 	boundary="----=_Part_3431_12384933.1139387792352"
        /// multipart/report; report-type=delivery-status; boundary="k04G6HJ9025016.1136391237/carbon.singnet.com.sg"
        /// application/pdf; name==?windows-1251?B?xODp5Obl8fIg7+7r/Ofu4uDy5ev88ero9SDq7uzo8uXy7uIgw/Dz7+/7?=
        /// </summary>
        public ContentType ContentType { get; set; }

        /// <summary>
        /// .NET framework combines MediaType (text) with subtype (plain) in one property, but
        /// often one or the other is needed alone. MediaMainType in this example would be 'text'.
        /// </summary>
        public string MediaMainType { get; set; }

        /// <summary>
        /// .NET framework combines MediaType (text) with subtype (plain) in one property, but
        /// often one or the other is needed alone. MediaSubType in this example would be 'plain'.
        /// </summary>
        public string MediaSubType { get; set; }

        /// <summary>
        /// MimeMessage can be used for any MIME entity, as a normal message body, an attachement or an alternative view. ContentStream
        /// provides the actual content of that MIME entity. It's mainly used internally and later mapped to the corresponding 
        /// .NET types.
        /// </summary>
        public Stream ContentStream { get; set; }

        /// <summary>
        /// A MIME entity can contain several MIME entities. A MIME entity has the same structure
        /// like an email. 
        /// </summary>
        public List<MimeMessage> Entities { get; set; }

        /// <summary>
        /// This entity might be part of a parent entity
        /// </summary>
        public MimeMessage Parent { get; set; }

        /// <summary>
        /// The top most MIME entity this MIME entity belongs to (grand grand grand .... parent)
        /// </summary>
        public MimeMessage TopParent { get; set; }

        /// <summary>
        /// Headerlines not interpretable by this class
        /// <example></example>
        /// </summary>
        public List<string> UnknowHeaderlines { get; set; } //

        /// <summary>
        /// Returns true if the message has attachments
        /// </summary>
        public bool HasAttachments => Attachments.Count > 0;

        #endregion Public properties

        #region Public methods

        /// <summary>
        /// Set all content type related fields
        /// </summary>
        public void SetContentTypeFields(string contentTypeString = null)
        {
            //set content type
            if (string.IsNullOrEmpty(contentTypeString))
            {
                ContentType = new ContentType()
                {
                    MediaType = MediaTypeNames.Text.Plain,
                    CharSet = "us-ascii"
                };
            }
            else
            {
                ContentType = new ContentType(contentTypeString.Trim()
                    .Replace(" = ", "="));
            }

            //set encoding (character set)
            if (ContentType.CharSet == null)
            {
                BodyEncoding = Encoding.ASCII;
            }
            else
            {
                BodyEncoding = Encoding.GetEncoding(ContentType.CharSet);
            }

            //set media main and sub type
            if (string.IsNullOrEmpty(ContentType.MediaType))
            {
                //no mediatype found
                ContentType.MediaType = MediaTypeNames.Text.Plain;
            }
            else
            {
                string mediaTypeString = ContentType.MediaType.Trim().ToLowerInvariant();
                int slashPosition = ContentType.MediaType.IndexOf("/");

                if (slashPosition < 1)
                {
                    //only main media type found
                    MediaMainType = mediaTypeString;
                    System.Diagnostics.Debugger.Break(); //didn't have a sample email to test this

                    if (MediaMainType == "text")
                    {
                        MediaSubType = "plain";
                    }
                    else
                    {
                        MediaSubType = "";
                    }
                }
                else
                {
                    //also submedia found
                    MediaMainType = mediaTypeString.Substring(0, slashPosition);

                    if (mediaTypeString.Length > slashPosition)
                    {
                        MediaSubType = mediaTypeString.Substring(slashPosition + 1);
                    }
                    else
                    {
                        if (MediaMainType == "text")
                        {
                            MediaSubType = "plain";
                        }
                        else
                        {
                            MediaSubType = "";
                            System.Diagnostics.Debugger.Break(); //didn't have a sample email to test this
                        }
                    }
                }
            }

            IsBodyHtml = MediaSubType.Equals("html");
        }

        /// <summary>
        /// Creates an empty child MIME entity from the parent MIME entity.
        /// 
        /// An email can consist of several MIME entities. A entity has the same structure
        /// like an email, that is header and body. The child inherits few properties 
        /// from the parent as default value.
        /// </summary>
        public MimeMessage CreateChildEntity()
        {
            MimeMessage child = new MimeMessage
            {
                Parent = this,
                TopParent = this.TopParent,
                ContentTransferEncoding = this.ContentTransferEncoding
            };

            return child;
        }

        /// <summary>
        /// Save this attachment to a file.
        /// </summary>
        /// <param name="attachment">Attachment to save.</param>
        /// <param name="path">Path to a file.</param>
        public void SaveAttachmentToFile(Attachment attachment, string path)
        {
            //byte[] allBytes = new byte[attachment.ContentStream.Length];
            //int bytesRead = attachment.ContentStream.Read(allBytes, 0, (int)attachment.ContentStream.Length);

            //string destinationFile = @"C:\" + attachment.Name;

            //BinaryWriter writer = new BinaryWriter(new FileStream(destinationFile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None));
            //writer.Write(allBytes);
            //writer.Close();

            using (var file = File.OpenWrite(path))
            {
                attachment.ContentStream.CopyTo(file);
            }
        }

        /// <summary>
        /// Save all attachments to files in a folder.
        /// </summary>
        /// <param name="path">Path to a folder for files.</param>
        public void SaveAttachmentsToFolder(string path)
        {
            foreach (var attachment in Attachments)
            {
                SaveAttachmentToFile(attachment, Path.Combine(path, attachment.Name));
            }
        }

        #endregion Public methods

        #region Debug info

        private StringBuilder _mailStructure;

        private void AppendLine(string format, object arg)
        {
            if (arg != null)
            {
                string argString = arg.ToString();

                if (argString.Length > 0)
                {
                    _mailStructure.AppendLine(string.Format(format, argString));
                }
            }
        }

        private void DecodeEntity(MimeMessage entity)
        {
            AppendLine("From  : {0}", entity.From);
            AppendLine("Sender: {0}", entity.Sender);
            AppendLine("To    : {0}", entity.To);
            AppendLine("CC    : {0}", entity.CC);
            AppendLine("ReplyT: {0}", entity.ReplyToList);
            AppendLine("Subj  : {0}", entity.Subject);
            AppendLine("S-Enc : {0}", entity.SubjectEncoding);

            if (entity.DeliveryDate > DateTime.MinValue)
            {
                AppendLine("Date  : {0}", entity.DeliveryDate);
            }

            if (entity.Priority != MailPriority.Normal)
            {
                AppendLine("Priory: {0}", entity.Priority);
            }

            if (entity.Body.Length > 0)
            {
                AppendLine("Body  : {0} byte(s)", entity.Body.Length);
                AppendLine("B-Enc : {0}", entity.BodyEncoding);
            }
            else
            {
                if (entity.BodyEncoding != Encoding.ASCII)
                {
                    AppendLine("B-Enc : {0}", entity.BodyEncoding);
                }
            }

            AppendLine("T-Type: {0}", entity.TransferType);
            AppendLine("C-Type: {0}", entity.ContentType);
            AppendLine("C-Desc: {0}", entity.ContentDescription);
            AppendLine("C-Disp: {0}", entity.ContentDisposition);
            AppendLine("C-Id  : {0}", entity.ContentId);
            AppendLine("M-ID  : {0}", entity.MessageId);
            AppendLine("Mime  : Version {0}", entity.MimeVersion);

            if (entity.ContentStream != null)
            {
                AppendLine("Stream: Length {0}", entity.ContentStream.Length);
            }

            //decode all child MIME entities
            foreach (MimeMessage child in entity.Entities)
            {
                _mailStructure.AppendLine("------------------------------------");
                DecodeEntity(child);
            }

            if (entity.ContentType != null && 
                entity.ContentType.MediaType != null && 
                entity.ContentType.MediaType.StartsWith("multipart"))
            {
                AppendLine("End {0}", entity.ContentType.ToString());
            }
        }

        /// <summary>
        /// Convert structure of message into a string
        /// </summary>
        /// <returns>Debug text</returns>
        public string MailStructure()
        {
            _mailStructure = new StringBuilder(1000);
            DecodeEntity(this);
            _mailStructure.AppendLine("====================================");

            return _mailStructure.ToString();
        }

        #endregion Debug info
    }
}
