using System;
using System.Reflection;
using System.Windows.Forms;
using ExceptionReporting.Views;

// https://github.com/PandaWood/ExceptionReporter.NET
// ReSharper disable UnusedMember.Global

namespace ExceptionReporting
{
	/// <summary>
	/// The entry-point (class) to invoking an ExceptionReporter dialog
	/// eg new ExceptionReporter().Show(exceptions)
	/// </summary>
	public class ExceptionReporter
	{
		private readonly ExceptionReportInfo _reportInfo;
		private IExceptionReportView _view;

		/// <summary>
		/// Initialise the ExceptionReporter
		/// </summary>
		public ExceptionReporter()
		{
			var callingAssembly = Assembly.GetCallingAssembly();

			_reportInfo = new ExceptionReportInfo { AppAssembly = callingAssembly };
		}

		// One issue we have with Config property here is that we store the exception and other info on it as well
		// This prevents us from allowing code like this new ExceptionReporter { Config = new ExceptionReportInfo { A = 1 } } 
		// which I would much prefer
		// TODO eventually allow this code above  
		
		/// <summary>
		/// Public access to configuration/settings
		/// </summary>
		public ExceptionReportInfo Config
		{
			get { return _reportInfo; }
		}

		/// <summary>
		/// Show the ExceptionReport dialog
		/// </summary>
		/// <remarks>The <see cref="ExceptionReporter"/> will analyze the <see cref="Exception"/>s and 
		/// create and show the report dialog.</remarks>
		/// <param name="exceptions">The <see cref="Exception"/>s to show.</param>
		public void Show(params Exception[] exceptions)
		{
			if (exceptions == null) return;		// silently ignore this mistake of passing null - user won't care

			try
			{
				_reportInfo.SetExceptions(exceptions);
				_view = new ExceptionReportView(_reportInfo);
				_view.ShowExceptionReport();
			}
			catch (Exception internalException)
			{
				MessageBox.Show(internalException.Message, "Failed while trying to report an Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		/// <summary>
		/// Show the ExceptionReport dialog with a custom message instead of the Exception's Message property
		/// </summary>
		/// <param name="customMessage">custom (exception) message</param>
		/// <param name="exceptions">The exception/s to display in the exception report</param>
		public void Show(string customMessage, params Exception[] exceptions)
		{
			_reportInfo.CustomMessage = customMessage;
			Show(exceptions);
		}

		/// <summary>
		/// Send the report without showing a dialog (silent send)
		/// <see cref="ReportSendMethod"/>must be set to SMTP or WebService, else this is ignored (silently)
		/// </summary>
		/// <param name="exceptions">The exception/s to include in the report</param>
		public void Send(params Exception[] exceptions)
		{
			_reportInfo.SetExceptions(exceptions);
			var generator = new ExceptionReportGenerator(_reportInfo);

			if (_reportInfo.SendMethod == ReportSendMethod.WebService)
			{
				generator.SendReportToWebService();
			} 
			else if (_reportInfo.SendMethod == ReportSendMethod.SMTP ||
			    _reportInfo.MailMethod == ExceptionReportInfo.EmailMethod.SMTP)		// backwards compatibility
			{
				generator.SendReportByEmail();
			}
			else if (_reportInfo.SendMethod == ReportSendMethod.SimpleMAPI ||
			         _reportInfo.MailMethod == ExceptionReportInfo.EmailMethod.SimpleMAPI)		// backwards compatibility
			{	// this option must be last for compatibility because osbsolete MailMethod.SimpleMAPI was previously 0/default
				// can't do silently so do nothing
			}
		}

		static readonly bool _isRunningMono = System.Type.GetType("Mono.Runtime") != null;

		/// <returns><c>true</c>, if running mono <c>false</c> otherwise.</returns>
		public static bool IsRunningMono() { return _isRunningMono; }

	}
}

// ReSharper restore UnusedMember.Global
