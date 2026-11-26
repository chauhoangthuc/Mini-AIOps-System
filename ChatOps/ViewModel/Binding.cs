using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ChatOps.Services;

namespace ChatOps.ViewModel;

public class Bindable : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void Notify([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        CommandManager.InvalidateRequerySuggested();
    }
    protected void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value)) { field = value; Notify(name); }
    }
}
public sealed class Command(Action<object?> run, Predicate<object?>? enabled = null) : ICommand
{
    public bool CanExecute(object? value) => enabled?.Invoke(value) ?? true;
    public void Execute(object? value) { if (CanExecute(value)) run(value); }
    public event EventHandler? CanExecuteChanged {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}
public abstract class Screen(GuardianApi api) : Bindable
{
    protected GuardianApi Api { get; } = api;
    private string message = "";
    public string Message { get => message; protected set => Set(ref message, value); }
    private bool busy;
    public bool Busy { get => busy; private set => Set(ref busy, value); }
    protected Command Work(Func<object?, Task> action, Predicate<object?>? condition = null) =>
        new(p => _ = Run(() => action(p)), p => !Busy && (condition?.Invoke(p) ?? true));
    public async Task Run(Func<Task> action)
    {
        if (Busy) return;
        Busy = true; Message = "Đang tải…";
        try { await action(); if (Message == "Đang tải…") Message = ""; }
        catch (OperationCanceledException) { Message = "Yêu cầu đã hủy hoặc quá thời gian. Hãy tải lại."; }
        catch (Exception error) { Message = error.Message; }
        finally { Busy = false; }
    }
}
