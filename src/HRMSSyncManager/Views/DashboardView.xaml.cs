using System.Windows;
using System.Windows.Controls;
using HRMSSyncManager.ViewModels;

namespace HRMSSyncManager.Views;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is DashboardViewModel vm && !string.IsNullOrEmpty(vm.SqlPassword))
            SqlPasswordBox.Password = vm.SqlPassword;
    }

    private void OnSqlPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is DashboardViewModel vm)
            vm.SqlPassword = SqlPasswordBox.Password;
    }
}
