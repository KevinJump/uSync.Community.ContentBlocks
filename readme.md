## ContentBlocks for uSync (or uSync for Content Blocks)

This package adds a [uSync Value mapper](https://docs.jumoo.co.uk/uSync/v8/extend/valuemapper/) and Dependency checker for [Perplex.ContentBlocks](https://our.umbraco.com/packages/backoffice-extensions/perplexcontentblocks/) to [uSync](https://github.com/KevinJump/uSync8).

## Do I need this ?
Maybe not - You may not need this mapper it does two things - so it depends on them.

### 1. Ensures DataTime formatting between servers
The content mapper just ensures that any DateTime properties that might be set within the nested content items are formatted correctly, this matters when your servers have different time formatting.  

### 2. Calculates Dependencies for uSync.Publisher
If you are using [uSync.Complete](https://jumoo.co.uk/usync/complete/) then you will want this mapper, it contains the Dependency Checker that allows uSync.Publisher to work out what elements are included 
as part of the content. 

That means what Media, other Document types or data types are needed to publish the item on a remote server. 

---
*This mapper is not part of uSync8.Community.Contrib because it has a dependency on Perplex.ContentBlocks, and we wanted to keep that only for people who needed it.*



