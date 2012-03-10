using System;
using System.Collections.ObjectModel;

namespace NITEProvider
{
    class PointCollection: KeyedCollection<int,PointStatus>
    {
        protected override int GetKeyForItem(PointStatus item)
        {
            return item.Handle;
        }
    }
}
