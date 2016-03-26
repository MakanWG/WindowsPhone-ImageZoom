using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

// The Templated Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234235

namespace ImageZoom
{
	public sealed class ImageViewer : Control
	{
		public ImageViewer()
		{
			this.DefaultStyleKey = typeof(ImageViewer);
		}

		public ImageSource Image
		{
			get { return (ImageSource)GetValue(ImageProperty); }
			set { SetValue(ImageProperty, value); }
		}

		public static readonly DependencyProperty ImageProperty =
			DependencyProperty.Register("Image", typeof(ImageSource), typeof(ImageViewer), null);

		private Grid Root;
		private ScrollViewer ScrollViewer;



		public double AppWidth
		{
			get { return (double)GetValue(AppWidthProperty); }
			set { SetValue(AppWidthProperty, value); }
		}

		// Using a DependencyProperty as the backing store for AppWidth.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty AppWidthProperty =
			DependencyProperty.Register("AppWidth", typeof(double), typeof(ImageViewer), new PropertyMetadata(ApplicationView.GetForCurrentView().VisibleBounds.Width));



		protected override void OnApplyTemplate()
		{
			Root = GetTemplateChild("Root") as Grid;
			ScrollViewer = GetTemplateChild("ScrollViewer") as ScrollViewer;
			//ScrollViewer.Unloaded += (s, e) =>
			//{
			//	ScrollViewer.ChangeView(0, 0, 1);
			//};
			//this.DoubleTapped += async (s, e) =>
			//{
			//	await Task.Delay(1);
			//	if (ScrollViewer.ZoomFactor == 2)
			//	{
			//		ScrollViewer.ChangeView(e.GetPosition(this).X, e.GetPosition(this).Y, 1);
			//	}
			//	else
			//	{
			//		ScrollViewer.ChangeView(e.GetPosition(this).X, e.GetPosition(this).Y, 2);
			//	}

			//};
			if (Root != null)
			{
				Root.ManipulationDelta += Root_ManipulationDelta;
				Root.ManipulationStarted += Root_ManipulationStarted;
				Root.ManipulationCompleted += Root_ManipulationCompleted;
				Root.RenderTransform = new CompositeTransform();
			}
			base.OnApplyTemplate();
		}

		private bool isscaling;

		private async void Root_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
		{
			e.Handled = true;
			await Task.Delay(10);
			isscaling = false;

		}

		private Point? lastOrigin;
		private double lastUniformScale;

		private void Root_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
		{
			lastUniformScale = Math.Sqrt(2);
			lastOrigin = null;
		}

		private void Root_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
		{
			var transform = Root.RenderTransform as CompositeTransform;
			if (transform != null)
			{
				var origin = e.Container.TransformToVisual(this).TransformPoint(e.Position);

				if (!lastOrigin.HasValue)
					lastOrigin = origin;

				//Calculate uniform scale factor
				double uniformScale = Math.Sqrt(Math.Pow(e.Cumulative.Scale, 2) +
												Math.Pow(e.Cumulative.Scale, 2));
				if (uniformScale == 0)
					uniformScale = lastUniformScale;

				//Current scale factor
				double scale = uniformScale / lastUniformScale;

				if (scale > 0 && scale != 1 && transform.ScaleX <= 4 && transform.ScaleX >= 1)
				{
					//Apply scaling
					transform.ScaleY = transform.ScaleX *= scale;
					//Update the offset caused by this scaling
					var ul = Root.TransformToVisual(this).TransformPoint(new Point());
					transform.TranslateX = origin.X - (origin.X - ul.X) * scale;
					transform.TranslateY = origin.Y - (origin.Y - ul.Y) * scale;
				}
				else if (scale > 0 && scale != 1 && transform.ScaleX >= 4 && scale < 1)
				{
					//Apply scaling
					transform.ScaleY = transform.ScaleX *= scale;
					//Update the offset caused by this scaling
					var ul = Root.TransformToVisual(this).TransformPoint(new Point());
					transform.TranslateX = origin.X - (origin.X - ul.X) * scale;
					transform.TranslateY = origin.Y - (origin.Y - ul.Y) * scale;
				}
				else if (scale > 0 && scale != 1 && transform.ScaleX <= 1 && scale > 1)
				{
					//Apply scaling
					transform.ScaleY = transform.ScaleX *= scale;
					//Update the offset caused by this scaling
					var ul = Root.TransformToVisual(this).TransformPoint(new Point());
					transform.TranslateX = origin.X - (origin.X - ul.X) * scale;
					transform.TranslateY = origin.Y - (origin.Y - ul.Y) * scale;
				}
				if (scale > 0 && scale != 1)
				{
					isscaling = true;
				}
				if (!isscaling)
				{
					//Apply translate caused by drag
					transform.TranslateX += (origin.X - lastOrigin.Value.X);
					transform.TranslateY += (origin.Y - lastOrigin.Value.Y);
				}


				//Cache values for next time
				lastOrigin = origin;
				lastUniformScale = uniformScale;
			}


		}
	}
}
