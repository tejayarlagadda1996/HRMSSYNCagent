using System.Windows;
using System.Windows.Controls;
using HRMSSyncManager.ViewModels;

namespace HRMSSyncManager.Views;

public partial class SetupWizardView : UserControl
{
    public SetupWizardView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is SetupWizardViewModel vm && !string.IsNullOrEmpty(vm.SqlPassword))
            SqlPasswordBox.Password = vm.SqlPassword;
    }

    private void OnSqlPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is SetupWizardViewModel vm)
            vm.SqlPassword = SqlPasswordBox.Password;
    }
}
