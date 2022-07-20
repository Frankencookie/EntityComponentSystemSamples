using Unity.Entities;
using UnityEngine;

struct FuelPickup : IComponentData {}

struct FuelPickupData : IComponentData
{
    public float FuelQuantity;
}

public class FuelPickupAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    [Header("Fuel Pickup Settings")]
    public float FuelQuantity = 50.0f;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponent<FuelPickup>(entity);

        dstManager.AddComponentData(entity, new FuelPickupData
        {
            FuelQuantity = FuelQuantity
        });
    }
}
