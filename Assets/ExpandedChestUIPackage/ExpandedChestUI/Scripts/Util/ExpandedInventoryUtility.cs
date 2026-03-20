using Inventory;
using Unity.Entities;
using Unity.Mathematics;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
namespace ExpandedChestUI.Util
{
    public static class ExpandedInventoryUtility
    {
        public static bool IsPlayerInventory;
        
        [SuppressMessage("ReSharper", "PossiblyImpureMethodCallOnReadonlyVariable")]
        public static void UpdateInventorySpace(
            in InventoryHandlerShared inventoryHandlerShared,
            Entity inventoryEntity,
            bool force = false)
        {
            if (!inventoryHandlerShared.inventoryLookup.TryGetBuffer(inventoryEntity, out var dynamicBuffer))
                return;
            for (int inventoryIndex = 0; inventoryIndex < dynamicBuffer.Length; ++inventoryIndex)
            {
                var inventory = dynamicBuffer[inventoryIndex];
                var extraInventorySpace = GetExtraInventorySpace(in inventoryHandlerShared, inventoryEntity, inventory);
                bool updateWholeInventory = (long)inventory.extraInventoryCategoryTagsMask !=
                                            (long)extraInventorySpace.categoryTagsMask;
                if (((force ? 1 : (inventory.extraSize != extraInventorySpace.size ? 1 : 0)) |
                     (updateWholeInventory ? 1 : 0)) == 0) continue;
                var position = inventoryHandlerShared.localTransformLookup[inventoryEntity].Position;
                InventoryUtility.UpdateInventoryRequirements(in inventoryHandlerShared, inventoryEntity,
                    extraInventorySpace, inventoryIndex);
                dynamicBuffer[inventoryIndex] = InventoryUtility.UpdateInventorySize(in inventoryHandlerShared,
                    inventoryEntity, extraInventorySpace, position, dynamicBuffer[inventoryIndex], inventoryIndex,
                    updateWholeInventory);
            }
        }

        [SuppressMessage("ReSharper", "PossiblyImpureMethodCallOnReadonlyVariable")]
        private static ExtraInventoryCD GetExtraInventorySpace(
            in InventoryHandlerShared inventoryHandlerShared,
            Entity inventoryEntity,
            InventoryBuffer inventory)
        {
            int inventorySizeSlot = inventory.extraInventorySizeSlot;
            if (inventorySizeSlot < 0)
                return new ExtraInventoryCD();
            var objectData = inventoryHandlerShared.containedObjectsBufferLookup[inventoryEntity][inventorySizeSlot]
                .objectData;
            if (objectData.objectID == ObjectID.None)
                return new ExtraInventoryCD();
            var levelEntity = EntityUtility.GetLevelEntity(
                PugDatabase.GetPrimaryPrefabEntity(objectData.objectID,
                    inventoryHandlerShared.databaseBankCD.databaseBankBlob, objectData.variation), objectData,
                inventoryHandlerShared.levelEntitiesBufferLookup, inventoryHandlerShared.levelLookup);
            return !inventoryHandlerShared.extraInventorySizeLookup.TryGetComponent(levelEntity,
                out var extraInventoryCd)
                ? new ExtraInventoryCD()
                : extraInventoryCd;
        }

        [SuppressMessage("ReSharper", "PossiblyImpureMethodCallOnReadonlyVariable")]
        public static void MoveAllItems(in InventoryHandlerShared inventoryHandlerShared, Entity inventoryFrom,
            Entity inventoryTo, bool isFromPlayerInventory, bool isToPlayerInventory)
        {
            if (InventoryUtility.CheckIfCanOnlyContainOneItemPerSlot(
                    inventoryHandlerShared.inventoryLookup[inventoryTo]))
                return;
            if (isToPlayerInventory) IsPlayerInventory = true;
            bool buffer =
                inventoryHandlerShared.lockedObjectsBufferLookup.TryGetBuffer(inventoryFrom, out var dynamicBuffer1);
            var inventoryBuffer1 = inventoryHandlerShared.inventoryLookup[inventoryFrom];
            foreach (var bufferItem in inventoryBuffer1)
            {
                int size = bufferItem.size;
                var dynamicBuffer3 = inventoryHandlerShared.containedObjectsBufferLookup[inventoryFrom];
                for (int startIndex = bufferItem.startIndex +
                                      (bufferItem.startIndex == 0 && isFromPlayerInventory ? 10 : 0);
                     startIndex < bufferItem.startIndex + size;
                     ++startIndex)
                {
                    if (buffer && dynamicBuffer1[startIndex].Value) continue;
                    var objectData = dynamicBuffer3[startIndex].objectData;
                    if (objectData.objectID != ObjectID.None)
                        InventoryUtility.TryMoveAll(in inventoryHandlerShared, inventoryFrom, startIndex, inventoryTo,
                            -1, -1, isQuickStacking: true);
                }
            }
            IsPlayerInventory = false;
        }

        [SuppressMessage("ReSharper", "PossiblyImpureMethodCallOnReadonlyVariable")]
        public static void SplitStack(in InventoryHandlerShared inventoryHandlerShared, Entity inventory)
        {
            if (!inventoryHandlerShared.containedObjectsBufferLookup.TryGetBuffer(inventory,
                    out var containedObjectsBuffers) ||
                !inventoryHandlerShared.inventoryLookup.TryGetBuffer(inventory, out var inventoryBuffers))
                return;
            InventoryUtility.Sort(in inventoryHandlerShared, inventory, false);
            foreach (var inv in inventoryBuffers)
            {
                int amountOfItems = 0;
                bool allSlotsTaken = true;
                int start = inv.startIndex;
                int end = start + inv.size;
                for (int i = start; i < end; i++)
                {
                    var slot = containedObjectsBuffers[i];
                    bool isStackable = PugDatabase.GetEntityObjectInfo(slot.objectID,
                        inventoryHandlerShared.databaseBankCD.databaseBankBlob, slot.variation).isStackable;
                    if (slot.objectID != ObjectID.None)
                    {
                        amountOfItems += isStackable ? slot.amount : 1;
                    }
                    else
                    {
                        allSlotsTaken = false;
                    }
                }

                if (allSlotsTaken) continue;
                int size = math.min(amountOfItems, inv.size) - 1;
                int moveTo = size + start;
                for (int i = size + start; i >= start;)
                {
                    var slot = containedObjectsBuffers[i];
                    bool isStackable = PugDatabase.GetEntityObjectInfo(slot.objectID,
                        inventoryHandlerShared.databaseBankCD.databaseBankBlob, slot.variation).isStackable;
                    int amount = isStackable ? slot.amount : 1;
                    if (moveTo == i && slot.objectID != ObjectID.None) break;
                    if (amount > 0 && slot.objectID != ObjectID.None)
                    {
                        InventoryUtility.MoveAmount(in inventoryHandlerShared, inventory, i, inventory, moveTo, -1, 1);
                        moveTo--;
                    }
                    else
                    {
                        i--;
                    }
                }
            }
        }
    }
}