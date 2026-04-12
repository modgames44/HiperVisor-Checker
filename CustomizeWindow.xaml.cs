using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BypassCheckerWPF;

public partial class CustomizeWindow : Window
{
    private SystemChecker _checker;
    private List<FeatureControl> _features = new List<FeatureControl>();

    public CustomizeWindow(SystemChecker checker)
    {
        InitializeComponent();
        _checker = checker;
        LoadFeatures();
    }

    private void LoadFeatures()
    {
        _features = new List<FeatureControl>
        {
            new FeatureControl("VBS (Virtualization-based Security)", 
                () => _checker.IsVbsEnabled(),
                () => _checker.EnableVbs(), 
                () => _checker.DisableVbs()),
            
            new FeatureControl("HVCI (Memory Integrity)", 
                () => _checker.IsHvciEnabled(),
                () => _checker.EnableHvci(), 
                () => _checker.DisableHvci()),
            
            new FeatureControl("Windows Hello Protection", 
                () => _checker.IsWindowsHelloEnabled(),
                () => _checker.EnableWindowsHello(), 
                () => _checker.DisableWindowsHello()),
            
            new FeatureControl("Enhanced Sign-in Security (SecureBiometrics)", 
                () => _checker.IsSecureBiometricsEnabled(),
                () => _checker.EnableSecureBiometrics(), 
                () => _checker.DisableSecureBiometrics()),
            
            new FeatureControl("System Guard Secure Launch", 
                () => _checker.IsSystemGuardEnabled(),
                () => _checker.EnableSystemGuard(), 
                () => _checker.DisableSystemGuard()),
            
            new FeatureControl("Credential Guard", 
                () => _checker.IsCredentialGuardEnabled(),
                () => _checker.EnableCredentialGuard(), 
                () => _checker.DisableCredentialGuard()),
            
            new FeatureControl("KVA Shadow (Meltdown Mitigation)", 
                () => _checker.IsKvaShadowEnabled(),
                () => _checker.EnableKvaShadow(), 
                () => _checker.DisableKvaShadow()),
            
            new FeatureControl("Windows Hypervisor", 
                () => _checker.IsHypervisorEnabled(),
                () => _checker.EnableHypervisor(), 
                () => _checker.DisableHypervisor())
        };

        foreach (var feature in _features)
        {
            var border = new Border { Style = (Style)FindResource("FeatureRow") };
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var nameText = new TextBlock 
            { 
                Text = feature.Name, 
                Foreground = (SolidColorBrush)FindResource("TextColor"),
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(nameText, 0);

            var statusText = new TextBlock
            {
                Text = feature.IsEnabled() ? "✅ ACTIVO" : "❌ INACTIVO",
                Foreground = feature.IsEnabled() ? Brushes.LightGreen : Brushes.LightCoral,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 10, 0)
            };
            Grid.SetColumn(statusText, 1);

            var togglePanel = new StackPanel { Orientation = Orientation.Horizontal };
            var btnEnable = new Button { Content = "Activar", Width = 80, Background = Brushes.Green };
            var btnDisable = new Button { Content = "Desactivar", Width = 80, Background = Brushes.DarkRed };
            btnEnable.Click += (s, e) => ToggleFeature(feature, true, statusText);
            btnDisable.Click += (s, e) => ToggleFeature(feature, false, statusText);
            togglePanel.Children.Add(btnEnable);
            togglePanel.Children.Add(btnDisable);
            Grid.SetColumn(togglePanel, 2);

            grid.Children.Add(nameText);
            grid.Children.Add(statusText);
            grid.Children.Add(togglePanel);
            border.Child = grid;
            stackFeatures.Children.Add(border);
        }
    }

    // Ejecuta la acción directamente sin preguntar, y actualiza el estado visual
    private void ToggleFeature(FeatureControl feature, bool enable, TextBlock statusText)
    {
        if (enable)
            feature.EnableAction();
        else
            feature.DisableAction();

        // Actualizar estado visual
        statusText.Text = feature.IsEnabled() ? "✅ ACTIVO" : "❌ INACTIVO";
        statusText.Foreground = feature.IsEnabled() ? Brushes.LightGreen : Brushes.LightCoral;
    }

    private void btnRebootNormal_Click(object sender, RoutedEventArgs e)
    {
        _checker.RebootSystem();
    }

    private void btnRebootAdvanced_Click(object sender, RoutedEventArgs e)
    {
        _checker.SetOneTimeAdvancedBoot();
        _checker.RebootSystem();
    }

    private void btnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

public class FeatureControl
{
    public string Name { get; }
    public Func<bool> IsEnabled { get; }
    public Action EnableAction { get; }
    public Action DisableAction { get; }

    public FeatureControl(string name, Func<bool> isEnabled, Action enable, Action disable)
    {
        Name = name;
        IsEnabled = isEnabled;
        EnableAction = enable;
        DisableAction = disable;
    }
}