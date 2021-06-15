# AnarchyBot
Worthless Discord Bot made for fun

# Commands
Default command prefix: `a!`

**Art group**
```
art setchannel [#channel] - Takes channel as argument. This is needed part for the group to work. Bot should has dedicated channel (In best case only it should be able to use it)
art publish [attachment] - [attachment] is a picture. Publishes it in beautified way, and optionally adds to databse
art upload [attachment] - [attachment] is a picture. Uploads the picture link to database. Further the picture can be picked via art rnd command.
art rnd - Returns random art from database (Sort by author and tags)
art tags - Displays all existing tags
```

<br>

Arguments:
publish, upload, rnd commands support arguments. Such as

<br>

```
-a, --author — Specifies author of art
-p, --profile — Adds link to author's social network
-t, --title — Specifies title of art
-f, --from — Specifies where is the character on art from
-s, --source — Direct link/url to picture, instead of using attachment
--tags — List of tags for the art. Separate tags with `;` symbol
--upload — Unique for publish command. Forces picture to be added to database and published.
```

<br>

Parameters usage:
If used short version (with one `-`) separate value with space. For values that consist of many words, put them into "". Examples: `-a "author name"`, `-a author`

<br>

If used longe version (with two `-`) separate value with `=` symbol. For values that consist of many words, put them into "". Examples: `--author="Author name"`, `--author=authorname`

<br>

***Command Examples***
```
a!art publish -t "Character name" -a "Author name" -p link_to_authors_social -s picture_source.png
a!art rnd --tags="tag1;tag2;tag number 3" --author="author name"
```

**Aottg bots module**

Commands related to indie game calld Aottg by Feng Lee<br>
Can join rooms of this game and allows to have chat coversation from discord to the game.<br>
To write something in chat, just send a message in discord channel.<br>
There is possible to have only 1 bot instace per discord server<br>
Bot also disconnects after 3 minutes of being inactive.

```
connect [eu/us/asia/jp] — Connects to selected game region. Must be executed first
join [roomNumber] — Joins room with [roomNumber]
dc — Disconnects bot from room
```

<br>

***Example of how it should be executed***
```
a!connect eu     //Now waiting for connect. Room list is displayed
a!join 2         //Joining with number 2 from room list
a!dc             //Leave the room bot were inside
```

<br>

# Build
Requirements:<br>
C# 8.0 or laster<br>
.NET Core 3.1   
<br>
Configuration file is needed (Name: "config.json"). (Place near output exe)<br>
The file should follow next scheme:
```json
//config.json
{
  "DiscordToken": "YourDiscordBotTokenHere", //Your bot's token
  "Database": "SQL Server",
  "ConnectionString": "Server=(localdb)\\mssqllocaldb;Database=your_database_name;Trusted_Connection=True;" //Database connection string
}
```

