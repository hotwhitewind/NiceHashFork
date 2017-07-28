using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interactivity;

namespace NewStyleMiner.Utils
{
    public class CloseWindowBehavior : Behavior<Window>
    {
        public bool CloseTrigger
        {
            get { return (bool)GetValue(CloseTriggerProperty); }
            set { SetValue(CloseTriggerProperty, value); }
        }

        public static readonly DependencyProperty CloseTriggerProperty =
            DependencyProperty.Register("CloseTrigger", typeof(bool), typeof(CloseWindowBehavior), new PropertyMetadata()
            {
                DefaultValue = false,
                PropertyChangedCallback = (obj, e) =>
                {
                    var behavior = obj as CloseWindowBehavior;
                    if(behavior != null)
                    {
                        behavior.OnCloseTriggerChanged();
                    }
                }
            });

        private void OnCloseTriggerChanged()
        {
            if(this.CloseTrigger)
            {
                this.AssociatedObject.Close();
            }
        }

        public bool ShowTrigger
        {
            get { return (bool)GetValue(ShowTriggerProperty); }
            set { SetValue(ShowTriggerProperty, value); }
        }

        public static readonly DependencyProperty ShowTriggerProperty =
            DependencyProperty.Register("ShowTrigger", typeof(bool), typeof(CloseWindowBehavior), new PropertyMetadata()
            {
                DefaultValue = false,
                PropertyChangedCallback = (obj, e) =>
                {
                    var behavior = obj as CloseWindowBehavior;
                    if (behavior != null)
                    {
                        behavior.OnShowTriggerChanged();
                    }
                }
            });

        private void OnShowTriggerChanged()
        {
            if (this.CloseTrigger)
            {
                this.AssociatedObject.Show();
            }
        }

    }
}
