# FarmhouseVisits
A mod that allows villagers to visit you.

## Features
You can add a blacklist via config; this will be read every time you load a save (and every time your farmer goes to sleep).
You can make characters visit you at any time you want- just edit `mistyspring.farmhousevisits/Schedules` via ContentPatcher

Example:

```
{
  "LogName": "Add George visit",
  "Action":"EditData",
  "Target":"mistyspring.farmhousevisits/Schedules",
  "Entries": {
    "George": 
    {
      "From": 700,
      "To": 1000,
      "EntryDialogue": "Hi, @. What are you young kids getting up to?#$b#I came to visit.",
      "ExitDialogue": "It's getting cold...#$b# I have to go. Goodbye, @.",
      "Dialogues":
      [
        "What's that over there?#$b#A picture you drew?", 
        "This house isn't so bad...", 
        "%George is looking at you as you work.",
      ]
    },
  "When":{
    "Hearts:George": 5
    }
  }
```

Empty template:
```
{

  "LogName": "Add visit",
  "Action":"EditData",
  "Target":"mistyspring.farmhousevisits/Schedules",
  "Entries": {
    "": 
    {
      "From": ,
      "To": ,
      "EntryDialogue": "",
      "ExitDialogue": "",
      "Dialogues":
      [
        "", 
        "", 
        "",
      ]
    }
  }
```

## For more info
You can add as many dialogues as you want, but they'll be only updated every 10 minutes.
If you don't set a finish to the visit ("To"), they'll go away when the regular visit time ends.


[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/G2G7CXX9P)
