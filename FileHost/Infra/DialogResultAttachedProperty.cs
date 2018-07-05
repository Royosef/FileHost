using System;
using System.Windows;

namespace FileHost.Infra
{
    public class DialogResultAttachedProperty
    {
        public static readonly DependencyProperty DialogResultProperty =
            DependencyProperty.RegisterAttached("DialogResult", typeof(bool?), typeof(DialogResultAttachedProperty), new PropertyMetadata(default(bool?), OnDialogResultChanged));

        private static void OnDialogResultChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is Window wnd))
                return;

            wnd.DialogResult = (bool?)e.NewValue;
        }

        public static bool? GetDialogResult(DependencyObject dp)
        {
            if (dp == null) throw new ArgumentNullException(nameof(dp));

            return (bool?)dp.GetValue(DialogResultProperty);
        }

        public static void SetDialogResult(DependencyObject dp, object value)
        {
            if (dp == null) throw new ArgumentNullException(nameof(dp));

            dp.SetValue(DialogResultProperty, value);
        }
    }
}
