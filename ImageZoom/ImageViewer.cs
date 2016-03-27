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
		private Image ImageControl;



		public double AppWidth
		{
			get { return (double)GetValue(AppWidthProperty); }
			set { SetValue(AppWidthProperty, value); }
		}

		// Using a DependencyProperty as the backing store for AppWidth.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty AppWidthProperty =
			DependencyProperty.Register("AppWidth", typeof(double), typeof(ImageViewer), new PropertyMetadata(ApplicationView.GetForCurrentView().VisibleBounds.Width));





		public double MaxZoomFactor
		{
			get { return (double)GetValue(MaxZoomFactorProperty); }
			set { SetValue(MaxZoomFactorProperty, value); }
		}

		// Using a DependencyProperty as the backing store for MaxZoomFactor.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty MaxZoomFactorProperty =
			DependencyProperty.Register("MaxZoomFactor", typeof(double), typeof(ImageViewer), new PropertyMetadata(10.0));



		public double MinZoomFactor
		{
			get { return (double)GetValue(MinZoomFactorProperty); }
			set { SetValue(MinZoomFactorProperty, value); }
		}

		// Using a DependencyProperty as the backing store for MinZoomFactor.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty MinZoomFactorProperty =
			DependencyProperty.Register("MinZoomFactor", typeof(double), typeof(ImageViewer), new PropertyMetadata(1.0));






		protected override void OnApplyTemplate()
		{
			Root = GetTemplateChild("Root") as Grid;
			ImageControl = GetTemplateChild("Image") as Image;
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

				if (scale > 0 && scale != 1) //scaling
				{
					isscaling = true;
					if ((transform.ScaleX > MinZoomFactor && transform.ScaleX < MaxZoomFactor) ||
						(transform.ScaleX >= MaxZoomFactor && scale < 1) ||
						(transform.ScaleX <= MinZoomFactor && scale > 1))
					{
						//Apply scaling
						transform.ScaleY = transform.ScaleX *= scale;
						//Update the offset caused by this scaling
						var ul = Root.TransformToVisual(this).TransformPoint(new Point());
						transform.TranslateX = origin.X - (origin.X - ul.X) * scale;
						transform.TranslateY = origin.Y - (origin.Y - ul.Y) * scale;
					}
				}

				var imageWidth = ImageControl.ActualWidth * transform.ScaleX;
				var imageHeight = ImageControl.ActualHeight * transform.ScaleY;
				var translateX = (origin.X - lastOrigin.Value.X);
				var translateY = (origin.Y - lastOrigin.Value.Y);
				var imageOffsetPoint = ImageControl.TransformToVisual(Root).TransformPoint(new Point(0, 0));
				if (!isscaling && transform.ScaleX > 1)//translating
				{
					if (imageWidth > Root.ActualWidth) // image is larger than width, translate horizontally
					{
						if ((translateX > 0 && transform.TranslateX <= 0) || 
							(translateX < 0 && transform.TranslateX + imageWidth >= ImageControl.ActualWidth))
						{
							transform.TranslateX += (origin.X - lastOrigin.Value.X);
						}
					}
					if (imageHeight > Root.ActualHeight) // image is bigger than height, translate vertically
					{
						if ((translateY > 0 && transform.TranslateY + (imageOffsetPoint.Y * transform.ScaleY) <= 0)||
							(translateY < 0 && transform.TranslateY + imageHeight + (imageOffsetPoint.Y * transform.ScaleY) >= Root.ActualHeight))
						{
							transform.TranslateY += (origin.Y - lastOrigin.Value.Y);
						}
					}
				}

				//Cache values for next time
				lastOrigin = origin;
				lastUniformScale = uniformScale;
			}


		}
	}
}
