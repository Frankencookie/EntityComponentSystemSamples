using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using UnityEngine.UI;

public class FuelUIHandler : MonoBehaviour, IReceiveEntity
{
    public Slider fuelUISlider;

    private Entity currentVehicle = Entity.Null;
    private List<Entity> vehicles = new List<Entity>();
    private EntityManager entityManager;

    void Start()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
    }

    void Update()
    {
        if (currentVehicle.Equals(Entity.Null))
            return;

        if (!entityManager.HasComponent<ActiveVehicle>(currentVehicle))
        {
            foreach (var item in vehicles)
            {
                if (entityManager.HasComponent<ActiveVehicle>(item))
                {
                    currentVehicle = item;
                }
            }
        }

        var fuelData = entityManager.GetComponentData<VehicleFuel>(currentVehicle);

        fuelUISlider.maxValue = fuelData.MaxFuel;
        fuelUISlider.value = fuelData.CurrentFuel;
    }

    public void SetReceivedEntity(Entity entity)
    {
        vehicles.Add(entity);
        currentVehicle = entity;
    }
}
