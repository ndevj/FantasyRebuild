using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Game Manager script
public class Player : MonoBehaviour
{
    public int happiness { get; private set; }
    [SerializeField] private float happyRange;  //at this percentage or higher, town is considered "happy"
    [SerializeField] private float sadRange;    //at this percentage or lower, town is considered "sad"
    public int population;
    public int buildingScore;
    public float minDistanceBetweenBuildings = 5.0f; // Minimum distance between buildings
    public List<Building> buildingList = new List<Building>(); // Initialize the list
    public List<House> houseList = new List<House>();        //List of all houses for population functions
    //TODO need to make an array/list of type Transform that holds all positions of buildings placed

    public float totalProductionBoost;
    public int nodeChargesPerCollect;       //how many charges are removed from a resource node when it's clicked

    //single instances of other classes
    public static DayCycle daycycle;
    public static Inventory inventory;
    public static Transform[] buildingPosArray;

    // References to building prefabs
    public GameObject housePrefab;
    public GameObject farmPrefab;
    public GameObject turretPrefab;
    public GameObject tavernPrefab;
    public GameObject magicBuildingPrefab;
    public GameObject woodBuildingPrefab;
    public GameObject stoneBuildingPrefab;

    #region Singleton
    public static Player instance;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
        }
        else
            instance = this;

        DontDestroyOnLoad(gameObject);
    }
    #endregion

    private void Start()
    {
        //set references to other classes
        inventory = Inventory.instance;
        daycycle = DayCycle.instance;

        List<Transform> buildingPositions = new List<Transform>();
        foreach (Building building in FindObjectsOfType<Building>())
        {
            buildingPositions.Add(building.transform);
            buildingList.Add(building); // Add existing buildings to the list
        }
        buildingPosArray = buildingPositions.ToArray();
    }

    //triggers node to collect their resources
    public void CollectResourceNode(ResourceNode node)
    {
        node.RemoveResource(nodeChargesPerCollect);
    }

    //triggers building to collect their resources
    public void CollectResourceBuilding(ResourceBuilding building)
    {
        building.Collect();
    }

    public void PlaceBuilding(Building building, Vector2 position)
    {
        if (CanPlaceBuilding(position))
        {
            // Check if the player has enough resources
            if (inventory.wood >= building.woodCost && inventory.stone >= building.stoneCost && inventory.magic >= building.magicCost)
            {
                // Deduct resources
                Purchase(building.woodCost, building.stoneCost, building.magicCost);

                // Instantiate the building at the specified position
                Instantiate(building, position, Quaternion.identity);

                // Add the building to the list
                AddBuildingList(building);

                // Add it to the House list if it's a house
                if (building is House)
                    AddHouseList(building.GetComponent<House>());

                // Add the building's position to the array
                List<Transform> buildingPositions = new List<Transform>(buildingPosArray);
                buildingPositions.Add(building.transform);
                buildingPosArray = buildingPositions.ToArray();

                // Add the building's score
                AddBuildingScore(building.score);
            }
            else
            {
                Debug.Log("Not enough resources to place the building.");
            }
        }
        else
        {
            Debug.Log("Cannot place building too close to another building.");
        }
    }
    public bool CanPlaceBuilding(Vector2 position)
    {
        foreach (Transform buildingPos in buildingPosArray)
        {
            if (Vector2.Distance(position, buildingPos.position) < minDistanceBetweenBuildings)
            {
                return false;
            }
        }
        return true;
    }

    //removes given resources from inventory
    public void Purchase(int woodCost, int stoneCost, int magicCost)
    {
        inventory.RemoveWood(woodCost);
        inventory.RemoveStone(stoneCost);
        inventory.RemoveMagic(magicCost);
    }

    //Determines how many population should be added or removed based on Player�s happiness and the housing space available in buildings
    public void AdjustPopulation()
    {
        float percent = HappinessPercent();
        int newPop = population;  //holds value that will be the population

        //sad
        if (percent < sadRange)
        {
            //figure out how many to remove until happiness is within "normal" range
            while (HappinessPercent(newPop) < sadRange)
            {
                newPop -= 1;
            }
        }
        
        //happy
        else if (percent >= happyRange)
        {
            //figure out how many to add until happiness is within "normal" range
            while (HappinessPercent(newPop) > happyRange)
            {
                newPop += 1;
            }
        }

        //normal - do nothing
        else
        {
            //continue on
        }

        if (newPop <= 0)
        {
            Debug.Log("Lost all population");
        }

        RedistributePopulation(newPop);   //now that there's enough people, make sure they're evenly distributed 
    }

    public float HappinessPercent()
    {
        return happiness / population;
    }

    //used to calculate theoretical percent with a new population value
    public float HappinessPercent(int newPopulation)
    {
        return happiness / newPopulation;
    }

    //Evenly distribute population over every house in buildingList
    //if there's more population than is possible to house, ignore them
    public void RedistributePopulation(int newPopulation)
    {
        int totalCapacity = 0;

        //how many people can be housed
        foreach (House house in houseList)
        {
            totalCapacity += house.capacity;
        }

        //discard extra people
        if (newPopulation > totalCapacity)
            newPopulation = totalCapacity;

        //set value of population
        SetPopulation(newPopulation);

        //distribute into houses
        int i = population;

        foreach (House house in houseList)
        {
            //there's enough to fill the house
            int cap = house.capacity;
            if (i >= cap)
            {
                house.SetOccupation(house.capacity);
                i -= house.capacity;
            }

            //not enough to fill the house
            else
            {
                house.SetOccupation(i);
                i = 0;
            }
        }
    }

    #region Add & Remove from variables

    public void AddHappiness(int amount) => happiness += amount;
    public void SubtractHappiness(int amount) => happiness -= amount;
    public void AddBuildingScore(int score) => buildingScore += score;
    public void SubtractBuildingScore(int score) => buildingScore -= score;
    public void AddBuildingList(Building building)
    {
        buildingList.Add(building);
    }
    public void RemoveBuildingList(Building building)
    {
        buildingList.Remove(building);
    }
    public void AddHouseList(House house)
    {
        houseList.Add(house);
    }
    public void RemoveHouseList(House house)
    {
        houseList.Remove(house);
    }
    public void AddProductionBoost(float amount) => totalProductionBoost += amount;
    public void SubtractProductionBoost(float amount) => totalProductionBoost -= amount;
    void SetPopulation(int newPopulation)
    {
        population = newPopulation;
    }

    #endregion

    public void CalculateScore()
    {
        //TODO
    }


}
