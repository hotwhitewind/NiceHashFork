using NewStyleMiner.AdditionalDialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace NewStyleMiner.Utils
{
    public class ButtonHelper
    {
        public enum ExDialogResultType { OK, YES, NO, CANCEL, RETRY };

        public static string GetDialogResult(DependencyObject obj) { return (string)obj.GetValue(DialogResultProperty); }
        public static void SetDialogResult(DependencyObject obj, string value) { obj.SetValue(DialogResultProperty, value); }
        public static readonly DependencyProperty DialogResultProperty = DependencyProperty.RegisterAttached("DialogResult", typeof(string), typeof(ButtonHelper), new UIPropertyMetadata
        {
            PropertyChangedCallback = (obj, e) =>
            {
                Button button = obj as Button;
                if (button == null)
                    throw new InvalidOperationException("Can only use ButtonHelper.DialogResult on button control");
                button.Click += (sender, e2) =>
                {
                    Window.GetWindow(button).Close();
                };
            }
        });
    }
}
