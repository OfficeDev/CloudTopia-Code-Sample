USE [socialevents_db]
GO

/****** Object:  Table [dbo].[tblObjectGraph]    Script Date: 5/13/2014 1:57:30 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[tblObjectGraph](
	[ObjectGraphID] [float] NOT NULL,
	[ObjectGraphUrl] [nvarchar](255) NOT NULL,
	[TwitterTags] [nvarchar](1024) NULL,
 CONSTRAINT [PK_tblObjectGraph] PRIMARY KEY CLUSTERED 
(
	[ObjectGraphID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
)

GO

/* To prevent any potential data loss issues, you should review this script in detail before running it outside the context of the database designer.*/
BEGIN TRANSACTION
SET QUOTED_IDENTIFIER ON
SET ARITHABORT ON
SET NUMERIC_ROUNDABORT OFF
SET CONCAT_NULL_YIELDS_NULL ON
SET ANSI_NULLS ON
SET ANSI_PADDING ON
SET ANSI_WARNINGS ON
COMMIT
BEGIN TRANSACTION
GO
ALTER TABLE dbo.tblObjectGraph ADD
	EventName nvarchar(255) NULL,
	EventDate datetime NULL
GO
ALTER TABLE dbo.tblObjectGraph SET (LOCK_ESCALATION = TABLE)
GO
COMMIT


USE [socialevents_db]
GO

INSERT INTO [dbo].[tblObjectGraph]
           ([ObjectGraphID]
           ,[ObjectGraphUrl]
           ,[TwitterTags]
		   ,[EventName]
		   ,[EventDate])
     VALUES
           (351561187046646
           ,'https://socialcoe.sharepoint.com/Shared%20Documents/3-tier%20architecture%20for%20Exchange%2012.doc'
           ,'speschka;tpeschka;', 'My Big Event', '12-13-2014')
GO

select * from tblObjectGraph

-- ==========================================================
-- Create Stored Procedure Template for SQL Azure Database
-- ==========================================================
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE addEvent 
	-- Add the parameters for the stored procedure here
	@ObjectGraphID float, @ObjectGraphUrl nvarchar(255), @TwitterTags nvarchar(1024), 
	@EventName nvarchar(255), @EventDate datetime
AS
BEGIN
    -- Insert statements for procedure here
	INSERT INTO [dbo].[tblObjectGraph]
           ([ObjectGraphID]
           ,[ObjectGraphUrl]
           ,[TwitterTags]
		   ,[EventName]
		   ,[EventDate])
     VALUES
           (@ObjectGraphID
           ,@ObjectGraphUrl
           ,@TwitterTags
		   ,@EventName
		   ,@EventDate)

END
GO


-- ==========================================================
-- Create Stored Procedure Template for SQL Azure Database
-- ==========================================================
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE deleteEvent 
	-- Add the parameters for the stored procedure here
	@ObjectGraphID float
AS
BEGIN
    -- Insert statements for procedure here
	DELETE FROM tblObjectGraph 
	WHERE ObjectGraphID = @ObjectGraphID
END
GO

-- ==========================================================
-- Create Stored Procedure Template for SQL Azure Database
-- ==========================================================
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE updateEventTags 
	-- Add the parameters for the stored procedure here
	@ObjectGraphID float, @TwitterTags nvarchar(1024)
AS
BEGIN
    -- Insert statements for procedure here
	UPDATE tblObjectGraph 
	SET TwitterTags = @TwitterTags 
	WHERE ObjectGraphID = @ObjectGraphID
END
GO

-- ==========================================================
-- Create Stored Procedure Template for SQL Azure Database
-- ==========================================================
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE getOneEvent
	-- Add the parameters for the stored procedure here
	@ObjectGraphID float
AS
BEGIN
    -- Insert statements for procedure here
	SELECT * FROM tblObjectGraph 
	WHERE ObjectGraphID = @ObjectGraphID
END
GO

-- ==========================================================
-- Create Stored Procedure Template for SQL Azure Database
-- ==========================================================
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE getAllEvents 
AS
BEGIN
    -- Insert statements for procedure here
	SELECT * FROM tblObjectGraph
END
GO


-- ================================================
-- Template generated from Template Explorer using:
-- Create Procedure (New Menu).SQL
--
-- Use the Specify Values for Template Parameters 
-- command (Ctrl-Shift-M) to fill in the parameter 
-- values below.
--
-- This block of comments will not be included in
-- the definition of the procedure.
-- ================================================
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE findEventByTag 
	-- Add the parameters for the stored procedure here
	@Tag nvarchar(1024)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for procedure here
	select * from tblObjectGraph 
	where TwitterTags LIKE @Tag
END
GO
