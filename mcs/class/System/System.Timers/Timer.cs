//
// System.Timers.Timer
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// (C) 2002 Ximian, Inc (http://www.ximian.com)
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// The docs talk about server timers and such...

//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System.ComponentModel;
using System.Threading;

namespace System.Timers
{
	[DefaultEventAttribute("Elapsed")]
	[DefaultProperty("Interval")]
	public class Timer : Component, ISupportInitialize
	{
		bool autoReset;
		bool enabled;
		double interval;
		ISynchronizeInvoke so;
		ManualResetEvent wait;
		Thread thread;
		object locker = new object ();

		[Category("Behavior")]
		[TimersDescription("Occurs when the Interval has elapsed.")]
		public event ElapsedEventHandler Elapsed;

		public Timer () : this (100)
		{
		}

		public Timer (double interval)
		{
			autoReset = true;
			enabled = false;
			Interval = interval;
			so = null;
			wait = null;
		}


		[Category("Behavior")]
		[DefaultValue(true)]
		[TimersDescription("Indicates whether the timer will be restarted when it is enabled.")]
		public bool AutoReset
		{
			get { return autoReset; }
			set { autoReset = value; }
		}

		[Category("Behavior")]
		[DefaultValue(false)]
		[TimersDescription("Indicates whether the timer is enabled to fire events at a defined interval.")]
		public bool Enabled
		{
			get { return enabled; }
			set {
				if (enabled == value)
					return;

				enabled = value;
				if (value) {
					wait = new ManualResetEvent (false);
					thread = new Thread (new ThreadStart (StartTimer));
					thread.IsBackground = true;
					thread.Start ();
				} else {
					StopTimer ();
				}
			}
		}

		[Category("Behavior")]
		[DefaultValue(100)]
		[RecommendedAsConfigurable(true)]
		[TimersDescription( "The number of milliseconds between timer events.")]
		public double Interval
		{
			get { return interval; }
			set { 
				// The doc says 'less than 0', but 0 also throws the exception
				if (value <= 0)
					throw new ArgumentException ("Invalid value: " + interval, "interval");

				interval = value;
			}
		}

		public override ISite Site
		{
			get { return base.Site; }
			set { base.Site = value; }
		}

		[DefaultValue(null)]
		[TimersDescriptionAttribute("The object used to marshal the event handler calls issued " +
					    "when an interval has elapsed.")]
#if NET_2_0
		[Browsable (false)]
#endif
		public ISynchronizeInvoke SynchronizingObject
		{
			get { return so; }
			set { so = value; }
		}

		public void BeginInit ()
		{
			// Nothing to do
		}

		public void Close ()
		{
			Enabled = false;
		}

		public void EndInit ()
		{
			// Nothing to do
		}

		public void Start ()
		{
			Enabled = true;
		}

		public void Stop ()
		{
			Enabled = false;
		}

		protected override void Dispose (bool disposing)
		{
			Close ();
			base.Dispose (disposing);
		}

		static void Callback (object state)
		{
			Timer timer = (Timer) state;
			if (timer.Elapsed == null)
				return;

			ElapsedEventArgs arg = new ElapsedEventArgs (DateTime.Now);

			if (timer.so != null && timer.so.InvokeRequired) {
				timer.so.BeginInvoke (timer.Elapsed, new object [2] {timer, arg});
			} else {
				timer.Elapsed (timer, arg);
			}
		}

		void StartTimer ()
		{
			WaitCallback wc = new WaitCallback (Callback);
			while (enabled && wait.WaitOne ((int) interval, false) == false) {
				if (autoReset == false)
					enabled = false;

				ThreadPool.QueueUserWorkItem (wc, this);
			}

			wc = null;

			lock (locker) {
				((IDisposable) wait).Dispose ();
				wait = null;
			}
		}

		void StopTimer ()
		{
			lock (locker) {
				if (wait != null)
					wait.Set ();
			}

			// the sleep speeds up the join under linux
			Thread.Sleep (0);
			thread.Join ();
		}
	}
}

