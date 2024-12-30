using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace BrickOrderHelper
{
    public class Element
    {
        public string Name { get; set; }
        public string ItemType { get; set; }
        public string ItemID { get; set; }
        public int BrickLinkColour { get; set; }
        public string Colour { get; set; }
        public double MaxPrice { get; set; }
        public int MinQty { get; set; }
        public string Condition { get; set; }
        public string Notify { get; set; }
        public double LegoPrice { get; set; }
        public double BrickLinkPrice { get; set; }
        public string AlternateItemIds
        {
            get
            {
                string itemIds = string.Empty;
                foreach (string itemId in _alternateItemIds)
                {
                    itemIds += $"{itemId}, ";
                }
                if (itemIds.Length > 0)
                {
                    itemIds = itemIds.Remove(itemIds.Length - 2);
                }
                return itemIds;
            }
        }
        private string[] _alternateItemIds = new string[0];
        public string[] AlternateIds { get { return _alternateItemIds; }
            set { _alternateItemIds = value; } }
    }
}
