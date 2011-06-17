using System.Windows.Media;

namespace Scrutiny.WPF
{
    public class ColorCombination
    {
        public Brush Background
        {
            get;
            set;
        }

        public Brush Foreground
        {
            get;
            set;
        }

        public Brush Border
        {
            get;
            set;
        }

        public ColorCombination(Brush background, Brush border)
        {
            Background = background;
            Border = border;
            Foreground = new SolidColorBrush(Colors.Black);
        }

        public ColorCombination(Brush background, Brush border, Brush foreground)
        {
            Background = background;
            Border = border;
            Foreground = foreground;
        }
    }
}