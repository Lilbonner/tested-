using System;
using UnityEngine;

namespace ScenarioBuilder
{
    /// <summary>
    /// Помечает поле с [SerializeReference]: в инспекторе появляется выпадающий список
    /// всех неабстрактных наследников типа поля. Так в редакторе выбираются условия шагов
    /// и правила режимов без написания кода.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SubclassPickerAttribute : PropertyAttribute { }

    /// <summary>Человекочитаемое имя типа в выпадающем списке.</summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DisplayNameAttribute : Attribute
    {
        public string Name { get; }
        public DisplayNameAttribute(string name) => Name = name;
    }
}
