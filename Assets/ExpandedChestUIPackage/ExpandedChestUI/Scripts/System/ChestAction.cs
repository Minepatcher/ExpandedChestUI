using Inventory;
using Rewired.Utils.Attributes;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;
using UnityEngine;
using ExpandedChestUI.Util;

// ReSharper disable once CheckNamespace
namespace ExpandedChestUI.Scripts.System
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(InventorySystemGroup))]
    public partial class ExpandedChestActionsClient : PugSimulationSystemBase
    {
        private NativeQueue<ExpandedChestActionRpc> _queue;
        private EntityArchetype _archetype;
        private BeginSimulationEntityCommandBufferSystem _ecbSystem;

        [Preserve]
        protected override void OnCreate()
        {
            _queue = new NativeQueue<ExpandedChestActionRpc>(Allocator.Persistent);
            _archetype = EntityManager.CreateArchetype(typeof(ExpandedChestActionRpc), typeof(SendRpcCommandRequest));
            _ecbSystem = World.GetOrCreateSystemManaged<BeginSimulationEntityCommandBufferSystem>();
            base.OnCreate();
        }

        [Preserve]
        protected override void OnDestroy()
        {
            _queue.Dispose();
            base.OnDestroy();
        }

        [Preserve]
        protected override void OnUpdate()
        {
            var commandBuffer = _ecbSystem.CreateCommandBuffer();
            while (_queue.TryDequeue(out var rpc1))
            {
                var entity = commandBuffer.CreateEntity(_archetype);
                commandBuffer.SetComponent(entity, rpc1);
            }
            base.OnUpdate();
        }

        public void MoveAllInventoryItems(Entity inventoryFrom, Entity inventoryTo, bool isFromPlayerInventory = false, bool isToPlayerInventory = false)
        {
            _queue.Enqueue(new ExpandedChestActionRpc
            {
                Action = ChestAction.MoveInventory,
                Inventory1 = inventoryFrom,
                Inventory2 = inventoryTo,
                Bool1 = isFromPlayerInventory,
                Bool2 = isToPlayerInventory
            });
        }
        
        public void SplitInventoryStacks(Entity inventory)
        {
            _queue.Enqueue(new ExpandedChestActionRpc
            {
                Action = ChestAction.Split,
                Inventory1 = inventory,
            });
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(InventorySystemGroup))]
    public partial class ExpandedChestActionsServer : PugSimulationSystemBase
    {
        private InventoryHandlerShared _inventoryHandlerShared;

        protected override void OnCreate()
        {
            RequireForUpdate<CraftBuffer>();
            RequireForUpdate<InventoryChangeBuffer>();
            RequireForUpdate<InventoryAuxDataSystemDataCD>();
            RequireForUpdate<WorldInfoCD>();
            RequireForUpdate<PugDatabase.DatabaseBankCD>();
            RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            RequireForUpdate<PhysicsWorldSingleton>();
            var entity = EntityManager.CreateEntity();
            EntityManager.AddBuffer<InventoryChangeBuffer>(entity);
            EntityManager.AddBuffer<CraftBuffer>(entity);
            EntityManager.AddBuffer<InventoryChangeResultBuffer>(entity);
            World.GetExistingSystemManaged<PredictedSimulationSystemGroup>().AddSystemToPartialTickUpdate(ref CheckedStateRef);
            base.OnCreate();
        }

        protected override void OnStartRunning()
        {
            _inventoryHandlerShared = new InventoryHandlerShared(ref CheckedStateRef,
                SystemAPI.GetSingleton<PugDatabase.DatabaseBankCD>(),
                SystemAPI.GetSingleton<SkillTalentsTableCD>(),
                SystemAPI.GetSingleton<UpgradeCostsTableCD>(), 
                SystemAPI.GetSingleton<InventoryAuxDataSystemDataCD>());
            base.OnStartRunning();
        }

        protected override void OnUpdate()
        { 
            var ecb = CreateCommandBuffer();
            var shared = _inventoryHandlerShared;
            Entities.WithAll<ExpandedChestActionRpc>().ForEach((Entity e, in ExpandedChestActionRpc rpc) =>
            {
                if (rpc.Inventory1 == Entity.Null)
                {
                    Debug.LogError("Got null inventory, are you sure it has a GhostComponent?");
                    return;
                }
                if (!shared.vendingMachineLookup.HasComponent(rpc.Inventory1) &&
                    (!shared.inventoryLookup.HasBuffer(rpc.Inventory1) ||
                     !shared.containedObjectsBufferLookup.HasBuffer(rpc.Inventory1)))
                    return;
                switch (rpc.Action)
                {
                    case ChestAction.MoveInventory:
                        ExpandedInventoryUtility.MoveAllItems(shared, rpc.Inventory1, rpc.Inventory2, rpc.Bool1, rpc.Bool2);
                        break;
                    case ChestAction.Split:
                        ExpandedInventoryUtility.SplitStack(shared, rpc.Inventory1);
                        break;
                    default:
                        return;
                }
                ExpandedInventoryUtility.UpdateInventorySpace(in shared, rpc.Inventory1);
                if (rpc.Inventory2 != Entity.Null)
                    ExpandedInventoryUtility.UpdateInventorySpace(in shared, rpc.Inventory2);
                ecb.DestroyEntity(e);
            })
            .WithName("ExtraChestAction")
            .WithBurst()
            .Schedule();
            base.OnUpdate();
        }
    }
    
    public enum ChestAction
    {
        MoveInventory,
        Split,
    }
    
    public struct ExpandedChestActionRpc : IRpcCommand
    {
        public ChestAction Action;
        public Entity Inventory1;
        public Entity Inventory2;
        public bool Bool1;
        public bool Bool2;
    }
}