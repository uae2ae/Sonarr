import React from 'react';
import FieldSet from 'Components/FieldSet';
import PageSectionContent from 'Components/Page/PageSectionContent';
import translate from 'Utilities/String/translate';
import { useSortedMetadataSources } from './useMetadataSources';
import MetadataSource from './MetadataSource';
import styles from './MetadataSources.css';

function MetadataSources() {
  const { data: items, isFetching, isFetched, error } = useSortedMetadataSources();

  return (
    <FieldSet legend={translate('MetadataSource')}>
      <PageSectionContent
        error={error}
        errorMessage={translate('MetadataLoadError')}
        isFetching={isFetching}
        isPopulated={isFetched}
      >
        <div className={styles.metadataSources}>
          {items.map((item) => (
            <MetadataSource key={item.id} {...item} />
          ))}
        </div>
      </PageSectionContent>
    </FieldSet>
  );
}

export default MetadataSources;
