using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class TimeManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    
    public static TimeManager Instance {get; private set; }

   [Header("Internal Clock")]
   [SerializeField]
   GameTimestamp timestamp;
   public float timeScale = 1.0f;


   [Header ("Day and Night cycle")]

   public Transform sunTransform;
   List<ITimeTracker> listeners = new List<ITimeTracker>();

   

      private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else{
            Instance = this;
        }

    }
    
    
    void Start()
    {
        timestamp = new GameTimestamp(0, GameTimestamp.Season.Spring, 1, 7, 0);
        StartCoroutine(TimeUpdate());
    }

    IEnumerator TimeUpdate()
    {
        while (true)
        {
               Tick();
      yield return new  WaitForSeconds(2/timeScale);
     
        }
       
    }


    public void Tick()
    {

       timestamp.UpdateClock();

        foreach(ITimeTracker listener in listeners)
        {
            listener.ClockUpdate(timestamp);
        }
        
       UpdateSunMovement();

      
    }


    void UpdateSunMovement()
    {
          int timeInMinutes = GameTimestamp.HoursToMinutes(timestamp.hour) + timestamp.minute;

        float sunAngle = .25f * timeInMinutes - 90;

        sunTransform.eulerAngles = new Vector3(sunAngle, 0, 0);
    }


    public GameTimestamp GetGameTimeStamp()
    {
        return new GameTimestamp(timestamp);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void RegisterTracker(ITimeTracker listener)
    {
        listeners.Add(listener);
    }

    public void UnregistTracker(ITimeTracker listener)
    {
        listeners.Remove(listener);
    }
}
