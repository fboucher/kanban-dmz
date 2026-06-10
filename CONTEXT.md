# Kanban

A kanban board for tracking work items with public and private visibility zones.

## Language

**Card**:
A unit of work tracked on the board with a title, description, status (column), assignee, tags, and a category.
_Avoid_: Task, ticket, item, sticky note

**Category**:
A single-value classification of a Card (e.g. Bug, Feature, Chore). Every Card has exactly one category. Categories are stored in a lookup table to support adding new ones over time.
_Avoid_: Type, label, kind

**Tag**:
A free-form label that can be attached to a Card. A Card can have zero or more tags.
_Avoid_: Label, marker

**Column**:
A named position on the board (e.g. Backlog, To Do, In Progress, Pending, Done). Cards are assigned to exactly one Column at a time. Columns are configurable.
_Avoid_: Status, lane, swimlane, stage

**Board**:
The top-level container for Cards and Columns. A Board has a visibility setting (public or private) that determines whether unauthenticated users can view it.

**Visibility**:
A property of a Board or Card that controls who can see it. A private Board requires authentication for all content. A public Board is visible to anyone, but individual Cards within it can be private, hiding their content from unauthenticated users. Every Card has two descriptions: a **public description** visible to everyone and a **private description** visible only to authenticated users.
_Avoid_: Security level, clearance, zone
