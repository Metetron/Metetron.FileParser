using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Parsnet.FileWatchers.WriteTimeWatcher.Data;

namespace Parsnet.Persistence.Configurations
{
    public class WriteTimeWatcherDataConfiguration : IEntityTypeConfiguration<WriteTimeWatcherData>
    {
        public void Configure(EntityTypeBuilder<WriteTimeWatcherData> builder)
        {
            builder.Property(wt => wt.ParserName)
                .IsRequired();

            builder.Property(wt => wt.LastWriteTimeUtc)
                .IsRequired()
                .HasDefaultValue(DateTime.MinValue.ToUniversalTime().Ticks);
        }
    }
}
