# EventMarketingEngine

---

Assumptions Made:
1. Since the scope does not include ticket sales, there was no need for a `Ticket` Model.
2. A customer can get to any event city directly. This can be visualised as a weighted graph with all nodes connected.
3. Expectation is to work with the class definitions for `Customer` and `Event`.
4. Expectation is to use the `GetDistance()` and `AddToEmail()` methods as provided.
5. There's only one event per city.
6. That all the events are **live** events and there is no time/schedule for the events; Events returned are only retrieved based on *distance* and *price*.

---

Tasks:

1.	**How would you call the AddToEmail method in order to send the events in an email?**
```C#
List<Event> events = GetEventsByCity(customer.City);

events.Sort(new EventPriceComparer());

foreach (var item in events)
{
    int? price = PriceServices.GetPrice(item);
    EmailServices.AddToEmail(customer, item, price);
}
```
2.	**As part of a new campaign, we need to be able to let customers know about events that are coming up close to their next birthday.**
```C#
var eventsCloseToBirthDate = events.Where(e => e.Date > DateTime.Now && e.Date.Month == customer.BirthDate.Month
    && e.Date.Year - DateTime.Now.Year < 2)
    .ToList();

eventsCloseToBirthDate.Sort(new EventDateComparer());

if (eventsCloseToBirthDate.Count > 0)
{
    int count = 0;

    int sendLimit = topN;

    foreach (Event e in eventsCloseToBirthDate)
    {
        count++;
        sendLimit--;
        SendCustomerNotifications(count, customer, e);

        if (sendLimit == 0)
            break;
    }
}
else
{
    Console.WriteLine($"No event close to {customer.Name}");
}
```                

3.	**Do you believe there is a way to improve the code you first wrote?** 
The code can be improved by any of the following:
- By adding a `Price` field to the `Event` class and reducing the overhead of computing it each time the email is sent.
- By adding a `Location` class and having Location Properties replace City in both `Customer` and `Event` classes. The `Location` class will include coordinates (`x` and `y`) and a function for computing the Manhattan distance between the Customer's city and the Event's city.
- This will serve as a more realistic solution for developing the above system.

4. **Write a code to add the 5 closest events to the customer's location to the email.**
**What should be your approach to getting the distance between the customer’s city and the other cities on the list?**
Two options were considered here:
- To define a 2-dimensional grid (e.g. 100 x 100) and calculate the Manhattan distance between City A and City B. The events are randomly placed on the grid (then transforming the x, y coordinates to fit within the 100 x 100 grid). With a larger grid size, this would be a more realistic approach.
- To use a 1-Dimensional space, where distances are directly computed using the character-length of the `City` and `Name` of the events. This appears to be a more straightforward approach for the scale of this project.

-	**How would you get the 5 closest events and how would you send them to the client in an email?**
To iterate through the events using the `PriorityQueue` data structure (Dictionary without the values), using the distance as the Priority parameter and 'taking' the top 5 results.
```C#
 PriorityQueue<Event, int> priorityQueue = new PriorityQueue<Event, int>();

foreach (Event e in events)
{
    var distance = GetDistance(e.City, customerCity);
    priorityQueue.Enqueue(e, distance);
}

int count = 0;

while (count < n)
{
    result.Add(priorityQueue.Dequeue());
    count++;
}           

  result.Sort(new EventDistanceComparer(customerCity));

```

-	**Do you believe there is a way to improve the code you first wrote?**
The above code can be made more efficient by caching, using a `Dictionary`, the computed distances using the Cities as key. This is done in the `GetDistance()` method.


5.	**If the GetDistance method is an API call which could fail or is too expensive, how will uimprove the code written in 2? Write the code.** 
By using a `Dictionary<key,int>` to cache the distances, where the key is a concatenation of the two cities i.e. 'CityACityB' for CityA and CityB.
```C#
 int numberOfTries = 5;

 while(numberOfTries > 0)
 {
     numberOfTries--;
     try
     {
         if (fromCity == null || toCity == null)
         {
             return 0;
         }

         if (fromCity.ToLower() == toCity.ToLower())
         {
             return 0;
         }

         // Bi-directional key
         string[] citiesArray = { fromCity, toCity };

         Array.Sort(citiesArray, (x, y) => x.CompareTo(y));

         string distanceCacheKey = String.Join('-',citiesArray);

         if (CachedDistances.ContainsKey(distanceCacheKey))
         {
             return CachedDistances[distanceCacheKey];
         }
        else
        {
            int computedDistance = GetAlphabeticalDistance(fromCity, toCity);
            CachedDistances.Add(distanceCacheKey, computedDistance);

            return computedDistance;
        }
         
     }
     catch (Exception)
     {
         // Log exception.
     }
}
```
6. **If the GetDistance method can fail, we don't want the process to fail. What can be done?
Code it. (Ask clarifying questions to be clear about what is expected business-wise).**
To ensure the process does not fail, we wrap the logic for computing distance in a `try-catch` and return 0 as the default. This allows us to still display the results to the Customers, but then sorted by `Price`.

7. **If we also want to sort the resulting events by other fields like price, etc. to determine whichones to send to the customer, how would you implement it? Code it.**
- We can add a `Price` field to the `Event` class OR
- Write a Price Comparer which is passed to `Sort` method.
```C#
class EventPriceComparer : IComparer<Event>
{
    public int Compare(Event? x, Event? y)
    {
        if(x == null || y == null)
        {
            return 0;
        }

        return PriceServices.GetPrice(x) - PriceServices.GetPrice(y);
    }
}
```
- The Comparer for sorting by distance uses the price as a fallback for situations where the distances are the same or events are in the same city
```C#
public class EventDistanceComparer:IComparer<Event>
{
    private string _relativeEventCity;

    public EventDistanceComparer(string relativeEventCity)
    {
        _relativeEventCity = relativeEventCity;
    }


    public int Compare(Event? x, Event? y)
    {
        if (x == null || y == null)
        {
            return 0;
        }

        int eventXDistance = 0;
        int eventYDistance = 0;

        if (_relativeEventCity != null)
        {
            eventXDistance = SpatialServices.GetDistance(_relativeEventCity, x.City);
            eventYDistance = SpatialServices.GetDistance(_relativeEventCity, y.City);
        }

        if (eventXDistance == eventYDistance)
        {
            int eventXPrice = PriceServices.GetPrice(x);
            int eventYPrice = PriceServices.GetPrice(y);

            return eventXPrice - eventYPrice;
        }

        return eventXDistance - eventYDistance;
    }
}
```
F. **One of the questions is: how do you verify that what you’ve done is correct.**
By using TDD. This approach can be used based on the requirements from the business.
