using System;
using System.ComponentModel;
using System.Security;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;

namespace Twoxzi.TeamViewerManager
{
    public class RelayCommand : ICommand
    {
        private Action<object> _action;
        private Predicate<object> canExecute;
        public RelayCommand(Action<object> action, Predicate<object> canExecute = null)
        {
            _action = action;
            this.canExecute = canExecute;
        }

        #region ICommand Members  
        public bool CanExecute(object parameter)
        {
            return canExecute?.Invoke(parameter) ?? true;
        }
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object parameter)
        {
            _action?.Invoke(parameter);
        }

        #endregion
    }
    /// <summary>
    /// 指定类型的RelayCommand
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RelayCommand<T> : ICommand where T : class
    {
        private Action<T> _action;
        private Predicate<T> canExecute;
        /// <summary>
        /// 如果不是目标类型时,控制是否抛出异常,当NotTargetTypeCallback事件不为空时,该属性失效
        /// </summary>
        public bool ThrowIfNotTargetType { get; set; }

        public RelayCommand(Action<T> action, Predicate<T> canExecute = null)
        {
            _action = action;
            this.canExecute = canExecute;
        }

        #region RelayCommand Members  
        public bool CanExecute(T parameter)
        {
            return canExecute?.Invoke(parameter) ?? true;
        }
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
        public void Execute(T parameter)
        {
            _action?.Invoke(parameter);
        }
        /// <summary>
        /// 当参数不是目标类型时的回调函数
        /// </summary>
        public event Action<Object> NotTargetTypeCallback;
        protected void OnNotTargetType(Object parameter)
        {
            if(NotTargetTypeCallback == null && ThrowIfNotTargetType)
            {
                throw new RelayCommandNotTargetTypeException("RelayCommand : NotTargetType");
            }
            else { NotTargetTypeCallback?.Invoke(parameter); }
        }

        #endregion

        #region ICommand

        event EventHandler ICommand.CanExecuteChanged
        {
            add
            {
                this.CanExecuteChanged += value;
            }

            remove
            {
                this.CanExecuteChanged -= value;
            }
        }

        void ICommand.Execute(object parameter)
        {
            if(parameter is T)
            {
                this.Execute(parameter as T);
            }
            else
            {
                OnNotTargetType(parameter);
            }
        }

        bool ICommand.CanExecute(object parameter)
        {
            if(parameter is T)
            {
                return this.CanExecute(parameter as T);
            }
            else
            {
                OnNotTargetType(parameter);
                return false;
            }
        }

        #endregion
    }

    public class RelayCommandNotTargetTypeException : Exception
    {
        public RelayCommandNotTargetTypeException(String message) : base(message)
        {

        }
        public RelayCommandNotTargetTypeException()
        {

        }
        public RelayCommandNotTargetTypeException(String message, Exception innerException) : base(message, innerException)
        {

        }
    }
}
