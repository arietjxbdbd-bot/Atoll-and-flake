using System.Windows;
using System.Windows.Controls;
namespace Flake.App;
public partial class MainWindow : Window
{
 public MainWindow() => InitializeComponent();
 private void Navigate_Click(object sender, RoutedEventArgs e)
 { if (sender is Button { Tag: string index } && int.TryParse(index, out var selected) && selected >= 0 && selected < Sections.Items.Count) Sections.SelectedIndex = selected; }
}
