using System;
using System.Net.Mail;
using ExceptionReporting.Core;
using Win32Mapi;

namespace ExceptionReporting.Mail
{
	internal class MailSender
	{
		public delegate void CompletedMethodDelegate(bool success);
		private readonly ExceptionReportInfo _reportInfo;

		internal MailSender(ExceptionReportInfo reportInfo)
		{
			_reportInfo = reportInfo;
		}

		/// <summary>
		/// Send SMTP email
		/// </summary>
		public void SendSmtp(string exceptionReport, CompletedMethodDelegate setEmailCompletedState)
		{
			var smtpClient = new SmtpClient(_reportInfo.SmtpServer) { DeliveryMethod = SmtpDeliveryMethod.Network };
			MailMessage mailMessage = CreateMailMessage(exceptionReport);

			smtpClient.SendCompleted += delegate { setEmailCompletedState.Invoke(true); };
			smtpClient.SendAsync(mailMessage, null);
		}

		/// <summary>
		/// Send SimpleMAPI email
		/// </summary>
		public void SendMapi(string exceptionReport, IntPtr windowHandle)
		{
			var mapi = new Mapi();
			mapi.Logon(windowHandle);
			mapi.Reset();

			string emailAddress = (string.IsNullOrEmpty(_reportInfo.EmailReportAddress))
			                      	? _reportInfo.ContactEmail
			                      	: _reportInfo.EmailReportAddress;

			mapi.AddRecipient(emailAddress, null, false);
			AttachMapiScreenshotIfRequired(mapi);
			mapi.Send(EmailSubject, exceptionReport, true);
			mapi.Logoff();
		}

		private void AttachMapiScreenshotIfRequired(Mapi mapi)
		{
			if (_reportInfo.ScreenshotAvailable)
				mapi.Attach(ScreenshotHelper.GetImageAsFile(_reportInfo.ScreenshotImage));
		}

		private MailMessage CreateMailMessage(string exceptionReport)
		{
			var mailMessage = new MailMessage
          	{
          		From = new MailAddress(_reportInfo.SmtpFromAddress, null),
          		ReplyTo = new MailAddress(_reportInfo.SmtpFromAddress, null),
          		Body = exceptionReport,
				Subject = EmailSubject
          	};

			mailMessage.To.Add(new MailAddress(_reportInfo.ContactEmail));
			AttachSmtpScreenshotIfRequired(mailMessage);

			return mailMessage;
		}

		private void AttachSmtpScreenshotIfRequired(MailMessage mailMessage)
		{
			if (_reportInfo.ScreenshotAvailable)
				mailMessage.Attachments.Add(
					new Attachment(ScreenshotHelper.GetImageAsFile(_reportInfo.ScreenshotImage), 
						ScreenshotHelper.ScreenshotMimeType));
		}

		private string EmailSubject
		{
			get { return string.Format(_reportInfo.TitleText + " for {0} v{1}", 
				_reportInfo.AppName, _reportInfo.AppVersion); }
		}
	}
}