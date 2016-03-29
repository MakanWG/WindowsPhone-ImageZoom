using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Security.Cryptography.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

// The Templated Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234235

namespace ImageZoom
{
    public sealed class ImageViewer : Control
    {
        private Grid _root;
        private Image _imageControl;

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

        public double AppWidth
        {
            get { return (double)GetValue(AppWidthProperty); }
            set { SetValue(AppWidthProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AppWidth.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AppWidthProperty =
            DependencyProperty.Register("AppWidth", typeof(double), typeof(ImageViewer), new PropertyMetadata(ApplicationView.GetForCurrentView().VisibleBounds.Width));


        public double AppHeight
        {
            get { return (double)GetValue(AppHeightProperty); }
            set { SetValue(AppHeightProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AppHeight.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AppHeightProperty =
            DependencyProperty.Register("AppHeight", typeof(double), typeof(ImageViewer), new PropertyMetadata(ApplicationView.GetForCurrentView().VisibleBounds.Height));



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
            _root = GetTemplateChild("Root") as Grid;
            _imageControl = GetTemplateChild("Image") as Image;
            if (_root != null)
            {
                _root.ManipulationDelta += Root_ManipulationDelta;
                _root.ManipulationStarted += Root_ManipulationStarted;
                _root.ManipulationCompleted += Root_ManipulationCompleted;
                _root.DoubleTapped += Root_DoubleTapped;
                _root.RenderTransform = new CompositeTransform();
            }
            base.OnApplyTemplate();
        }

        private void Root_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            HandleDoubleTap(e.GetPosition(_root));
        }

        private bool _isScaling;



        private void Root_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            e.Handled = true;
            _isScaling = false;
            ResetPosition();
        }

        private void ResetPosition()
        {
            var transform = _root.RenderTransform as CompositeTransform;
            var imageWidth = _imageControl.ActualWidth * transform.ScaleX;
            var imageHeight = _imageControl.ActualHeight * transform.ScaleY;
            var imageOffsetPoint = _imageControl.TransformToVisual(_root).TransformPoint(new Point(0, 0));
            if (transform.ScaleX < MinZoomFactor)
            {
                ResetScale();
            }
            if (transform.TranslateX >= 0 || transform.TranslateX + imageWidth <= _imageControl.ActualWidth)
            {
                ResetHorizontal(transform, imageWidth);
            }
            if (transform.TranslateY + (imageOffsetPoint.Y * transform.ScaleY) >= 0 || transform.TranslateY + imageHeight + (imageOffsetPoint.Y * transform.ScaleY) <= _root.ActualHeight)
            {
                ResetVertical(transform, imageHeight, imageOffsetPoint);
            }
            isZoomed = transform.ScaleX >= 1.005;
        }

        private void ResetScale()
        {
            var storyboard = new Storyboard();
            var doubleAnimationX = new DoubleAnimation() { To = MinZoomFactor, Duration = new Duration(TimeSpan.FromMilliseconds(200)) };
            var doubleAnimationY = new DoubleAnimation() { To = MinZoomFactor, Duration = new Duration(TimeSpan.FromMilliseconds(200)) };
            Storyboard.SetTarget(doubleAnimationY, _root);
            Storyboard.SetTargetProperty(doubleAnimationY, "(UIElement.RenderTransform).(CompositeTransform.ScaleY)");
            storyboard.Children.Add((doubleAnimationY));
            Storyboard.SetTarget(doubleAnimationX, _root);
            Storyboard.SetTargetProperty(doubleAnimationX, "(UIElement.RenderTransform).(CompositeTransform.ScaleX)");
            storyboard.Children.Add((doubleAnimationX));
            storyboard.Begin();
        }

        private void ResetHorizontal(CompositeTransform transform, double imageWidth)
        {
            double? resetTo = null;
            if (transform.TranslateX >= 0)
            {
                resetTo = 0.0;
            }
            else
            {
                resetTo = _imageControl.ActualWidth - imageWidth;
            }
            var storyboard = new Storyboard();
            var doubleAnimation = new DoubleAnimation() { To = resetTo, Duration = new Duration(TimeSpan.FromMilliseconds(200)) };
            Storyboard.SetTarget(doubleAnimation, _root);
            Storyboard.SetTargetProperty(doubleAnimation, "(UIElement.RenderTransform).(CompositeTransform.TranslateX)");
            storyboard.Children.Add((doubleAnimation));
            storyboard.Begin();
        }

        private void ResetVertical(CompositeTransform transform, double imageHeight, Point imageOffsetPoint)
        {
            double? resetTo = null;
            if (imageHeight > _root.ActualHeight)
            {
                if (transform.TranslateY + (imageOffsetPoint.Y * transform.ScaleY) <= 0)
                {

                    resetTo = _root.ActualHeight - imageHeight - (imageOffsetPoint.Y * transform.ScaleY);
                }
                else
                {
                    resetTo = -(imageOffsetPoint.Y * transform.ScaleY);
                }
            }
            else
            {
                resetTo = (AppHeight - (_root.ActualHeight * transform.ScaleX)) / 2; //center vertically if the image is smaller than it's bounds
            }


            var storyboard = new Storyboard();
            var doubleAnimation = new DoubleAnimation() { To = resetTo, Duration = new Duration(TimeSpan.FromMilliseconds(100)) };
            Storyboard.SetTarget(doubleAnimation, _root);
            Storyboard.SetTargetProperty(doubleAnimation, "(UIElement.RenderTransform).(CompositeTransform.TranslateY)");
            storyboard.Children.Add((doubleAnimation));
            storyboard.Begin();
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
            var transform = _root.RenderTransform as CompositeTransform;
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
                    _isScaling = true;
                    if ((transform.ScaleX > MinZoomFactor && transform.ScaleX < MaxZoomFactor) ||
                        (transform.ScaleX >= MaxZoomFactor && scale < 1) ||
                        (transform.ScaleX <= MinZoomFactor && scale > 1))
                    {
                        //Apply scaling
                        transform.ScaleY = transform.ScaleX *= scale;
                        //Update the offset caused by this scaling
                        var ul = _root.TransformToVisual(this).TransformPoint(new Point());
                        transform.TranslateX = origin.X - (origin.X - ul.X) * scale;
                        transform.TranslateY = origin.Y - (origin.Y - ul.Y) * scale;
                    }
                }

                var imageWidth = _imageControl.ActualWidth * transform.ScaleX;
                var imageHeight = _imageControl.ActualHeight * transform.ScaleY;
                var translateX = (origin.X - lastOrigin.Value.X);
                var translateY = (origin.Y - lastOrigin.Value.Y);
                var imageOffsetPoint = _imageControl.TransformToVisual(_root).TransformPoint(new Point(0, 0));
                if (!_isScaling && transform.ScaleX > 1)//translating
                {
                    if (imageWidth > _root.ActualWidth) // image is larger than width, translate horizontally
                    {
                        if ((translateX > 0 && transform.TranslateX <= 0) ||
                            (translateX < 0 && transform.TranslateX + imageWidth >= _imageControl.ActualWidth))
                        {
                            transform.TranslateX += (origin.X - lastOrigin.Value.X);
                        }
                    }
                    if (imageHeight > _root.ActualHeight) // image is bigger than height, translate vertically
                    {
                        if ((translateY > 0 && transform.TranslateY + (imageOffsetPoint.Y * transform.ScaleY) <= 0) ||
                            (translateY < 0 && transform.TranslateY + imageHeight + (imageOffsetPoint.Y * transform.ScaleY) >= _root.ActualHeight))
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

        private bool isZoomed;

        private void HandleDoubleTap(Point point)
        {
            if (isZoomed)
            {
                ZoomToNegativeScale(1, point);
            }
            else
            {
                ZoomToPositiveScale(2.0, point);
            }
        }

        private async Task ZoomToPositiveScale(double targetZoom, Point origin)
        {
            lastUniformScale = Math.Sqrt(2);
            lastOrigin = null;
            var initialZoom = (_root.RenderTransform as CompositeTransform).ScaleX;
            var step = (targetZoom) / 20;
            for (double i = initialZoom; i <= targetZoom; i += step)
            {
                await PerformZoom(origin, i);
            }

            ResetPosition();
        }

        private async Task ZoomToNegativeScale(double targetZoom, Point origin)
        {
            lastUniformScale = Math.Sqrt(2);
            lastOrigin = null;
            var initialZoom = (_root.RenderTransform as CompositeTransform).ScaleX;
            var step = (targetZoom / initialZoom) / 10;
            for (double i = 1; i >= (targetZoom / initialZoom); i -= step)
            {
                await PerformZoom(origin, i);
            }

            ResetPosition();
        }

        private async Task PerformZoom(Point origin, double zoomStep)
        {
            await Task.Delay(10);
            double uniformScale = Math.Sqrt(Math.Pow(zoomStep, 2) +
                                            Math.Pow(zoomStep, 2));
            if (uniformScale == 0)
                uniformScale = lastUniformScale;

            //Current scale factor
            double scale = uniformScale / lastUniformScale;
            var transform = _root.RenderTransform as CompositeTransform;
            //Apply scaling
            transform.ScaleY = transform.ScaleX *= scale;
            //Update the offset caused by this scaling
            var ul = _root.TransformToVisual(this).TransformPoint(new Point());
            transform.TranslateX = origin.X - (origin.X - ul.X) * scale;
            transform.TranslateY = origin.Y - (origin.Y - ul.Y) * scale;
            lastOrigin = origin;
            lastUniformScale = uniformScale;
        }
    }
}
