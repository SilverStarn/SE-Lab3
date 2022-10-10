using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Lab2Solution
{
    public interface IDatabase
    {
        void AddEntry(Entry entry);
        int GetNextId();
        bool DeleteEntry(Entry entry);
        Entry FindEntry(int id);
        ObservableCollection<Entry> GetEntries();
        bool EditEntry(Entry replacementEntry);
        bool SortByAnswer();
        bool SortByClue();
    }
}