# SquidORM
SquidORM is an easy to use C# ORM for MySql, using linq to sql and expression trees.

You just have to create a record that herit from DatabaseRecord class, and now you're free to use provided attributes to make it work !

This ORM support :
- Auto incremented key binding after insertion
- Relationship of different records, as a class, a list, an array even if a dictionary
- Auto Parent relation property updating when a child is a updated all it's parent have it's Foreign keys updated in server side.
- Foreign keys
and more...
