using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Mathematics;
using UnityEngine;


[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(ExportPhysicsWorld))]
[UpdateBefore(typeof(EndFramePhysicsSystem))]
public partial class TriggerFuelPickupSystem : SystemBase
{
    StepPhysicsWorld m_StepPhysicsWorldSystem;
    BuildPhysicsWorld m_BuildPhysicsWorld;

    //For testing if we have any fuel triggers
    EntityQuery m_TriggerFuelGroup;
    //For testing if we have any vehicles with fuel
    EntityQuery m_VehicleFuelGroup;

    EndSimulationEntityCommandBufferSystem endSimBufferSystem;

    //Grab all triggers
    protected override void OnCreate()
    {
        m_StepPhysicsWorldSystem = World.GetOrCreateSystem<StepPhysicsWorld>();
        m_BuildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
        endSimBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

        m_TriggerFuelGroup = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[]
            {
                typeof(FuelPickupData)
            }
        });

        m_VehicleFuelGroup = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[]
            {
                typeof(VehicleFuel)
            }
        });
    }

    protected override void OnUpdate()
    {
        //If there aren't any fuel triggers or vehicle's with fuel, return
        if (m_TriggerFuelGroup.CalculateEntityCount() == 0 || m_VehicleFuelGroup.CalculateEntityCount() == 0)
        {
            return;
        }

        var ecb = new EntityCommandBuffer(Allocator.TempJob);

        Dependency = new PickupFuelJob
        {
            TriggerFuelpickupGroup = GetComponentDataFromEntity<FuelPickupData>(true),
            VehicleFuelGroup = GetComponentDataFromEntity<VehicleFuel>(),
            ecbSystem = ecb
        }.Schedule(m_StepPhysicsWorldSystem.Simulation, Dependency);

        Dependency.Complete();

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        this.RegisterPhysicsRuntimeSystemReadOnly();
    }

    [BurstCompile]
    struct PickupFuelJob : ITriggerEventsJob
    {
        [ReadOnly] public ComponentDataFromEntity<FuelPickupData> TriggerFuelpickupGroup;
        public ComponentDataFromEntity<VehicleFuel> VehicleFuelGroup;
        public EntityCommandBuffer ecbSystem;

        public void Execute(TriggerEvent triggerEvent)
        {
            //The two entities we're testing
            Entity entityA = triggerEvent.EntityA;
            Entity entityB = triggerEvent.EntityB;

            bool isBodyATrigger = TriggerFuelpickupGroup.HasComponent(entityA);
            bool isBodyBTrigger = TriggerFuelpickupGroup.HasComponent(entityB);

            // Ignoring barrels overlapping other barrels
            if (isBodyATrigger && isBodyBTrigger)
                return;

            Entity fuelEntity;
            Entity triggerEntity;

            //Does A or B have fuel
            if (VehicleFuelGroup.HasComponent(entityA))
            {
                fuelEntity = entityA;
                triggerEntity = entityB;
            }
            else if (VehicleFuelGroup.HasComponent(entityB))
            {
                fuelEntity = entityB;
                triggerEntity = entityA;
            }
            else
            {
                return;
            }

            //Get components
            var vehicleFuelComponent = VehicleFuelGroup[fuelEntity];
            var fuelPickupComponent = TriggerFuelpickupGroup[triggerEntity];

            //Calculate new fuel
            float newFuel = vehicleFuelComponent.CurrentFuel + fuelPickupComponent.FuelQuantity;
            newFuel = math.clamp(newFuel, 0, vehicleFuelComponent.MaxFuel);

            vehicleFuelComponent.CurrentFuel = newFuel;

            //Set fuel data
            VehicleFuelGroup[fuelEntity] = vehicleFuelComponent;

            ecbSystem.DestroyEntity(triggerEntity);
        }
    }
}
