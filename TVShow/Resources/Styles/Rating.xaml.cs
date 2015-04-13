﻿using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace TVShow.Resources.Styles
{
    /// <summary>
    /// Interaction logic for Rating.xaml
    /// </summary>
    public partial class Rating : UserControl
    {
        public static readonly DependencyProperty RatingValueProperty = DependencyProperty.Register("RatingValue", typeof(int), typeof(Rating),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(RatingChanged)));

        private const int Max = 5;

        public int RatingValue
        {
            get
            {
                return (int)GetValue(RatingValueProperty);
            }
            set
            {
                if (value < 0)
                {
                    SetValue(RatingValueProperty, 0);
                }
                else if (value > Max)
                {
                    SetValue(RatingValueProperty, Max);
                }
                else
                {
                    SetValue(RatingValueProperty, value);
                }
            }
        }

        private static void RatingChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            Rating item = sender as Rating;
            if (item != null)
            {
                int newval = (int) e.NewValue;
                UIElementCollection childs = ((Grid) (item.Content)).Children;

                ToggleButton button;

                for (int i = 0; i < newval; i++)
                {
                    button = childs[i] as ToggleButton;
                    if (button != null)
                        button.IsChecked = true;
                }

                for (int i = newval; i < childs.Count; i++)
                {
                    button = childs[i] as ToggleButton;
                    if (button != null)
                        button.IsChecked = false;
                }
            }
        }

        public Rating()
        {
            InitializeComponent();
        }
    }
}
