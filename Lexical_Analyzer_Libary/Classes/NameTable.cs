using Lexical_Analyzer_Libary.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexical_Analyzer_Libary.Classes
{
    /// <summary>
    /// Класс для хранения и управления таблицей идентификаторов
    /// </summary>
    public class NameTable
    {
        private LinkedList<Identifier> identifiers;  // Связанный список для хранения идентификаторов

        public NameTable()
        {
            identifiers = new LinkedList<Identifier>();
        }

        /// <summary>
        /// Добавление идентификатора в таблицу
        /// </summary>
        /// <param name="name">Имя идентификатора</param>
        /// <param name="category">Категория идентификатора</param>
        /// <param name="type">Тип идентификатора (по умолчанию None)</param>
        /// <returns>Добавленный идентификатор</returns>
        /// <exception cref="Exception">Выбрасывается, если идентификатор с таким именем уже существует</exception>
        public Identifier AddIdentifier(string name, tCat category, tType type = tType.None)
        {
            if (FindByName(name).Name != null)
                throw new Exception($"Идентификатор с именем '{name}' уже существует.");

            Identifier identifier = new Identifier(name, type, category);
            identifiers.AddLast(identifier); 
            return identifier;
        }

        /// <summary>
        /// Поиск идентификатора по имени
        /// </summary>
        /// <param name="name">Имя идентификатора</param>
        /// <returns>Найденный идентификатор или пустой идентификатор, если не найден</returns>
        public Identifier FindByName(string name)
        {
            foreach (var identifier in identifiers)
            {
                if (identifier.Name == name)
                    return identifier;
            }
            return default;  
        }

        /// <summary>
        /// Получение всех идентификаторов в таблице
        /// </summary>
        /// <returns>Список идентификаторов</returns>
        public LinkedList<Identifier> GetIdentifiers()
        {
            return identifiers;
        }
    }
}
