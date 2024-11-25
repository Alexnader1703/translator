using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexical_Analyzer_Libary.Classes
{
    /// <summary>
    /// Перечисление типов данных идентификаторов
    /// </summary>
    public enum tType
    {
        None,  
        Int,   
        Bool,
        String, Float, Double, Char
    };

    /// <summary>
    /// Перечисление категорий идентификаторов
    /// </summary>
    public enum tCat
    {
        Const,  // Константа
        Var,    // Переменная
        Type    // Тип данных
    };

    /// <summary>
    /// Структура для хранения информации об идентификаторе
    /// </summary>
    public struct Identifier
    {
        public string Name;  // Имя идентификатора
        public tType Type;   // Тип идентификатора
        public tCat Category;  // Категория идентификатора

        public Identifier(string name, tType type, tCat category)
        {
            Name = name;
            Type = type;
            Category = category;
        }

        public override string ToString()
        {
            return $"Name: {Name}, Type: {Type}, Category: {Category}";
        }
    }
}
