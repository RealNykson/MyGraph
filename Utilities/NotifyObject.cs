using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MyGraph.Utilities
{
  public class NotifyObject : INotifyPropertyChanged
  {
    private readonly Dictionary<string, object> _propertyValues;

    protected NotifyObject()
    {
      _propertyValues = new Dictionary<string, object>();
    }

    protected virtual void Set<T>(T value, [CallerMemberName] string propertyName = null)
    {
      if (_propertyValues.ContainsKey(propertyName))
      {
        //Nur ändern, wenn es auch tatsächlich eine Änderung gab, bei null sollte der Wert danach immer von einer Value überschrieben werden
        if (_propertyValues[propertyName] == null || !_propertyValues[propertyName].Equals(value))
        {
          _propertyValues[propertyName] = value;
        }
      }
      else
      {
        _propertyValues.Add(propertyName, value);
      }
      OnPropertyChanged(propertyName);
    }

    protected T Get<T>([CallerMemberName] string propertyName = null)
    {
      if (_propertyValues.ContainsKey(propertyName))
      {
        return (T)_propertyValues[propertyName];
      }
      return default;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}
