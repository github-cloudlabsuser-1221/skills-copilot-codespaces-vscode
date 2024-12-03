CREATE PROCEDURE dbo.RefreshStatisticsOrRebuildIndexes
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TableName NVARCHAR(255);
    DECLARE @IndexName NVARCHAR(255);
    DECLARE @SQL NVARCHAR(MAX);
    DECLARE @Fragmentation FLOAT;
    DECLARE @ModificationCounter INT;

    -- Cursor to loop through all user tables
    DECLARE table_cursor CURSOR FOR
    SELECT QUOTENAME(SCHEMA_NAME(schema_id)) + '.' + QUOTENAME(name)
    FROM sys.tables;

    OPEN table_cursor;
    FETCH NEXT FROM table_cursor INTO @TableName;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        -- Cursor to loop through all indexes of the current table
        DECLARE index_cursor CURSOR FOR
        SELECT name
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(@TableName) AND type > 0;

        OPEN index_cursor;
        FETCH NEXT FROM index_cursor INTO @IndexName;

        WHILE @@FETCH_STATUS = 0
        BEGIN
            -- Get fragmentation level
            SET @SQL = 'SELECT @Fragmentation = avg_fragmentation_in_percent
                        FROM sys.dm_db_index_physical_stats(DB_ID(), OBJECT_ID(''' + @TableName + '''), INDEXPROPERTY(OBJECT_ID(''' + @TableName + '''), ''' + @IndexName + ''', ''IndexID''), NULL, ''LIMITED'')';
            EXEC sp_executesql @SQL, N'@Fragmentation FLOAT OUTPUT', @Fragmentation OUTPUT;

            -- Get modification counter
            SET @SQL = 'SELECT @ModificationCounter = modification_counter
                        FROM sys.dm_db_stats_properties(OBJECT_ID(''' + @TableName + '''), STATS_ID(''' + @IndexName + '''))';
            EXEC sp_executesql @SQL, N'@ModificationCounter INT OUTPUT', @ModificationCounter OUTPUT;

            -- Decide whether to refresh statistics or rebuild indexes
            IF @Fragmentation > 30
            BEGIN
                -- Rebuild indexes if fragmentation is greater than 30%
                SET @SQL = 'ALTER INDEX ' + QUOTENAME(@IndexName) + ' ON ' + @TableName + ' REBUILD';
                EXEC sp_executesql @SQL;
            END
            ELSE IF @ModificationCounter > 500
            BEGIN
                -- Refresh statistics if modification counter is greater than 500
                SET @SQL = 'UPDATE STATISTICS ' + @TableName + ' ' + QUOTENAME(@IndexName);
                EXEC sp_executesql @SQL;
            END

            FETCH NEXT FROM index_cursor INTO @IndexName;
        END;

        CLOSE index_cursor;
        DEALLOCATE index_cursor;

        FETCH NEXT FROM table_cursor INTO @TableName;
    END;

    CLOSE table_cursor;
    DEALLOCATE table_cursor;
END;
GO