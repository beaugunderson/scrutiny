using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

using Scrutiny.Extensions;

namespace Scrutiny.WPF
{
    /// <summary>
    /// Takes two values, one being a string and the other being a search term to highlight
    /// </summary>
    /// <returns>
    /// A <see cref="TextBlock"/> with Inlines collection filled with <see cref="Run"/> elements.
    /// </returns>
    public class HighlightConverter : IMultiValueConverter
    {
        private readonly ColorCombination[] _colorCombinations = new[]
        {
            new ColorCombination(new SolidColorBrush(Colors.GreenYellow), new SolidColorBrush(Colors.Green)), 
            new ColorCombination(new SolidColorBrush(Colors.LightPink), new SolidColorBrush(Colors.DarkRed)), 
            new ColorCombination(new SolidColorBrush(Colors.LightBlue), new SolidColorBrush(Colors.DarkBlue)), 
            new ColorCombination(new SolidColorBrush(Colors.Yellow), new SolidColorBrush(Colors.DarkOrange)) 
        };

        private class ColorMatch
        {
            public int ColorIndex
            {
                get;
                set;
            }

            public Match Match
            {
                get;
                set;
            }

            public ColorMatch(Match match, int colorIndex)
            {
                Match = match;
                ColorIndex = colorIndex;
            }
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)(DesignerProperties.IsInDesignModeProperty.GetMetadata(typeof(DependencyObject)).DefaultValue))
            {
                return values;
            }

            var value = System.Convert.ToString(values[0]);

            if (values[1] == DependencyProperty.UnsetValue)
            {
                return value;
            }

            var terms = (string)values[1];

            if (string.IsNullOrEmpty(terms))
            {
                return value;
            }

            // Highlight the search terms
            int count = 0;

            var colorMatches = new List<ColorMatch>();

            foreach (var term in terms.Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries))
            {
                var matches = Regex.Matches(value, Regex.Escape(term));

                colorMatches.AddRange(from Match match in matches select new ColorMatch(match, count));

                count++;
            }

            colorMatches.Sort(item => item.Match.Index);

            var textBlock = new TextBlock
            {
                Padding = new Thickness(0, 1, 0, 1)
            };

            int index = 0;

            foreach (var match in colorMatches)
            {
                textBlock.Inlines.Add(value.Slice(index, match.Match.Index));

                index = match.Match.Index + match.Match.Length;

                // TODO: Change look to a user setting
                var border = new Border
                {
                    Background = _colorCombinations[match.ColorIndex].Background,
                    BorderBrush = _colorCombinations[match.ColorIndex].Border,
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(0.5, -1, 0.5, -1),
                    CornerRadius = new CornerRadius(1),
                    Child = new TextBlock(new Run(match.Match.Value))
                    {
                        Foreground = _colorCombinations[match.ColorIndex].Foreground,
                        Padding = new Thickness(1, 0, 1, 0)
                    }
                };

                var container = new InlineUIContainer(border)
                {
                    BaselineAlignment = BaselineAlignment.Bottom,
                    FontWeight = FontWeights.Bold
                };

                textBlock.Inlines.Add(container);
            }

            textBlock.Inlines.Add(value.Slice(index, value.Length));

            return textBlock;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}